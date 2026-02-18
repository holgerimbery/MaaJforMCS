using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Configuration;
using CopilotStudioTestRunner.Domain.Entities;
using CopilotStudioTestRunner.Core.DirectLine;
using CopilotStudioTestRunner.Core.Evaluation;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CopilotStudioTestRunner.Core.Execution;

/// <summary>
/// Orchestrates test execution against Direct Line and evaluates responses
/// </summary>
public interface ITestExecutionService
{
    Task<Run> ExecuteTestSuiteAsync(
        Guid suiteId,
        TestRunnerDbContext dbContext,
        DirectLineClient directLineClient,
        IJudgeService judgeService,
        JudgeSetting judgeSettings,
        DirectLineSettings directLineSettings,
        int delayBetweenTestsMs = 2000,
        Guid? agentId = null,
        CancellationToken cancellationToken = default);
}

public class TestExecutionService : ITestExecutionService
{
    private readonly ILogger _logger = Log.ForContext<TestExecutionService>();

    public async Task<Run> ExecuteTestSuiteAsync(
        Guid suiteId,
        TestRunnerDbContext dbContext,
        DirectLineClient directLineClient,
        IJudgeService judgeService,
        JudgeSetting judgeSettings,
        DirectLineSettings directLineSettings,
        int delayBetweenTestsMs = 2000,
        Guid? agentId = null,
        CancellationToken cancellationToken = default)
    {
        var suite = await dbContext.TestSuites.FindAsync([suiteId], cancellationToken: cancellationToken);
        if (suite == null)
        {
            throw new InvalidOperationException($"Test suite {suiteId} not found");
        }

        var run = new Run
        {
            Id = Guid.NewGuid(),
            SuiteId = suiteId,
            AgentId = agentId,
            StartedAt = DateTime.UtcNow,
            Status = "running"
        };

        dbContext.Runs.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        var testCases = dbContext.TestCases
            .Where(tc => tc.SuiteId == suiteId && tc.IsActive)
            .ToList();

        _logger.Information("Starting execution of suite {SuiteName} with {TestCaseCount} test cases",
            suite.Name, testCases.Count);

        run.TotalTestCases = testCases.Count;
        var latencies = new List<long>();

        for (int i = 0; i < testCases.Count; i++)
        {
            var testCase = testCases[i];
            try
            {
                var result = await ExecuteTestCaseAsync(
                    testCase,
                    directLineClient,
                    judgeService,
                    judgeSettings,
                    directLineSettings,
                    cancellationToken);

                result.RunId = run.Id;
                run.Results.Add(result);
                dbContext.Results.Add(result);
                latencies.Add(result.LatencyMs);

                if (result.Verdict == "pass")
                    run.PassedCount++;
                else if (result.Verdict == "fail")
                    run.FailedCount++;
                else
                    run.SkippedCount++;

                _logger.Information("Test case {TestCaseName}: {Verdict}",
                    testCase.Name, result.Verdict);

                // Add delay between tests to prevent rate limiting (except after last test)
                if (i < testCases.Count - 1 && delayBetweenTestsMs > 0)
                {
                    _logger.Information("Waiting {DelayMs}ms before next test", delayBetweenTestsMs);
                    await Task.Delay(delayBetweenTestsMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error executing test case {TestCaseName}", testCase.Name);
                run.FailedCount++;

                // Add delay even on error
                if (i < testCases.Count - 1 && delayBetweenTestsMs > 0)
                {
                    await Task.Delay(delayBetweenTestsMs, cancellationToken);
                }
            }
        }

        // Calculate statistics
        if (latencies.Count > 0)
        {
            run.AverageLatencyMs = latencies.Average();
            run.MedianLatencyMs = CalculatePercentile(latencies, 50);
            run.P95LatencyMs = CalculatePercentile(latencies, 95);
        }

        run.CompletedAt = DateTime.UtcNow;
        run.Status = "completed";

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is Run)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues == null)
                    {
                        dbContext.Runs.Add(run);
                    }
                    else
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.Information(
            "Suite execution completed: {PassCount} passed, {FailCount} failed, {SkipCount} skipped",
            run.PassedCount, run.FailedCount, run.SkippedCount);

        return run;
    }

    private async Task<Result> ExecuteTestCaseAsync(
        TestCase testCase,
        DirectLineClient directLineClient,
        IJudgeService judgeService,
        JudgeSetting judgeSettings,
        DirectLineSettings directLineSettings,
        CancellationToken cancellationToken)
    {
        var maxRetries = directLineSettings.MaxRetries;
        var backoffSeconds = directLineSettings.BackoffSeconds;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await ExecuteTestCaseAttemptAsync(
                    testCase, 
                    directLineClient, 
                    judgeService, 
                    judgeSettings, 
                    directLineSettings, 
                    cancellationToken);

                // Check if the response contains a rate limit error
                var hasRateLimitError = result.TranscriptMessages.Any(m => 
                    m.Content?.Contains("GenAIToolPlannerRateLimitReached", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Content?.Contains("RateLimitReached", StringComparison.OrdinalIgnoreCase) == true);

                if (hasRateLimitError && attempt < maxRetries)
                {
                    var delayMs = (int)(Math.Pow(2, attempt) * backoffSeconds * 1000);
                    _logger.Warning(
                        "Rate limit error detected for test case {TestCaseName}. Attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms...",
                        testCase.Name, attempt + 1, maxRetries + 1, delayMs);
                    
                    await Task.Delay(delayMs, cancellationToken);
                    continue; // Retry
                }

                if (hasRateLimitError)
                {
                    result.Verdict = "error";
                    result.ErrorMessage = "Rate limit exceeded after all retry attempts";
                    _logger.Error(
                        "Rate limit error persisted for test case {TestCaseName} after {Attempts} attempts",
                        testCase.Name, attempt + 1);
                }

                return result;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delayMs = (int)(Math.Pow(2, attempt) * backoffSeconds * 1000);
                _logger.Warning(ex, 
                    "Test case {TestCaseName} failed on attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms...",
                    testCase.Name, attempt + 1, maxRetries + 1, delayMs);
                
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                // Final attempt failed
                _logger.Error(ex, "Test case {TestCaseName} failed after {Attempts} attempts", 
                    testCase.Name, attempt + 1);
                
                return new Result
                {
                    Id = Guid.NewGuid(),
                    TestCaseId = testCase.Id,
                    ExecutedAt = DateTime.UtcNow,
                    Verdict = "error",
                    ErrorMessage = $"Failed after {attempt + 1} attempts: {ex.Message}"
                };
            }
        }

        // Should not reach here, but just in case
        return new Result
        {
            Id = Guid.NewGuid(),
            TestCaseId = testCase.Id,
            ExecutedAt = DateTime.UtcNow,
            Verdict = "error",
            ErrorMessage = "Unexpected error: exceeded retry limit"
        };
    }

    private async Task<Result> ExecuteTestCaseAttemptAsync(
        TestCase testCase,
        DirectLineClient directLineClient,
        IJudgeService judgeService,
        JudgeSetting judgeSettings,
        DirectLineSettings directLineSettings,
        CancellationToken cancellationToken)
    {
        var result = new Result
        {
            Id = Guid.NewGuid(),
            TestCaseId = testCase.Id,
            ExecutedAt = DateTime.UtcNow,
            Verdict = "unknown"
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Start conversation
            var conversationId = await directLineClient.StartConversationAsync(cancellationToken);
            var transcript = new List<TranscriptMessage>();

            // Execute user inputs (conversation steps)
            var watermark = "";
            foreach (var userInput in testCase.UserInput)
            {
                await directLineClient.SendActivityAsync(conversationId, userInput, cancellationToken);

                // Collect responses with timeout
                var timeout = TimeSpan.FromSeconds(
                    testCase.TimeoutSeconds ?? directLineSettings.ReplyTimeoutSeconds);
                var pollStart = DateTime.UtcNow;

                while ((DateTime.UtcNow - pollStart) < timeout && !cancellationToken.IsCancellationRequested)
                {
                    var hasBotReply = false;
                    var (activities, nextWatermark) = await directLineClient.GetActivitiesAsync(conversationId, watermark, cancellationToken);

                    foreach (var activity in activities)
                    {
                        if (activity.Type == "message" && activity.From?.Id != "user")
                        {
                            var msg = new TranscriptMessage
                            {
                                Id = Guid.NewGuid(),
                                ResultId = result.Id,
                                Role = activity.From?.Name ?? activity.From?.Id ?? "bot",
                                Content = activity.Text ?? "",
                                Timestamp = activity.Timestamp,
                                SequenceNumber = transcript.Count,
                                RawActivityJson = System.Text.Json.JsonSerializer.Serialize(activity)
                            };
                            transcript.Add(msg);
                            hasBotReply = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(nextWatermark))
                    {
                        watermark = nextWatermark;
                    }

                    if (hasBotReply)
                    {
                        break;
                    }

                    if (activities.Count == 0)
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                }

                // Add user message to transcript
                var userMsg = new TranscriptMessage
                {
                    Id = Guid.NewGuid(),
                    ResultId = result.Id,
                    Role = "user",
                    Content = userInput,
                    Timestamp = DateTime.UtcNow,
                    SequenceNumber = transcript.Count
                };
                transcript.Insert(transcript.Count(m => m.Role == "user"), userMsg);
            }

            stopwatch.Stop();
            result.LatencyMs = stopwatch.ElapsedMilliseconds;
            result.TurnCount = testCase.UserInput.Length;

            // Evaluate using judge
            foreach (var msg in transcript)
            {
                result.TranscriptMessages.Add(msg);
            }

            var evaluation = await judgeService.EvaluateAsync(judgeSettings, testCase, transcript, cancellationToken);

            result.TaskSuccessScore = evaluation.TaskSuccess;
            result.IntentMatchScore = evaluation.IntentMatch;
            result.FactualityScore = evaluation.Factuality;
            result.HelpfulnessScore = evaluation.Helpfulness;
            result.SafetyScore = evaluation.Safety;
            result.Verdict = evaluation.Verdict;
            result.JudgeRationale = evaluation.Rationale;
            result.JudgeCitations = evaluation.Citations?.ToArray() ?? [];

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            result.LatencyMs = stopwatch.ElapsedMilliseconds;
            result.Verdict = "skipped";
            result.ErrorMessage = "Execution cancelled";
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.LatencyMs = stopwatch.ElapsedMilliseconds;
            result.Verdict = "error";
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Test case attempt execution error");
            throw; // Re-throw to allow retry logic to handle it
        }
    }

    private double CalculatePercentile(List<long> values, int percentile)
    {
        if (values.Count == 0) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)((percentile / 100.0) * sorted.Count);
        return sorted[Math.Min(index, sorted.Count - 1)];
    }
}
