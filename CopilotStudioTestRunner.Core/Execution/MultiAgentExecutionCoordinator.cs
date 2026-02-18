using CopilotStudioTestRunner.Core.DirectLine;
using CopilotStudioTestRunner.Core.Evaluation;
using CopilotStudioTestRunner.Core.Services;
using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Entities;
using CopilotStudioTestRunner.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CopilotStudioTestRunner.Core.Execution;

/// <summary>
/// Coordinator for executing tests against one or more agents
/// Handles creating agent-specific clients and configurations
/// </summary>
public interface IMultiAgentExecutionCoordinator
{
    /// <summary>
    /// Execute a test suite against a single agent
    /// </summary>
    Task<Run> ExecuteForAgentAsync(
        Guid suiteId,
        Agent agent,
        TestRunnerDbContext dbContext,
        IJudgeService judgeService,
        IAgentConfigurationService agentConfig,
        int delayBetweenTestsMs = 2000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a test suite against multiple agents
    /// </summary>
    Task<List<Run>> ExecuteForMultipleAgentsAsync(
        Guid suiteId,
        List<Agent> agents,
        TestRunnerDbContext dbContext,
        IJudgeService judgeService,
        IAgentConfigurationService agentConfig,
        int delayBetweenTestsMs = 2000,
        CancellationToken cancellationToken = default);
}

public class MultiAgentExecutionCoordinator : IMultiAgentExecutionCoordinator
{
    private readonly ITestExecutionService _executionService;
    private readonly ILogger _logger = Log.ForContext<MultiAgentExecutionCoordinator>();

    public MultiAgentExecutionCoordinator(ITestExecutionService executionService)
    {
        _executionService = executionService;
    }

    public async Task<Run> ExecuteForAgentAsync(
        Guid suiteId,
        Agent agent,
        TestRunnerDbContext dbContext,
        IJudgeService judgeService,
        IAgentConfigurationService agentConfig,
        int delayBetweenTestsMs = 2000,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Executing test suite {SuiteId} against agent {AgentName} ({AgentId})",
            suiteId, agent.Name, agent.Id);

        // Get agent-specific Direct Line configuration
        var (botId, secret, timeout, useWebSocket, useWebChannelSecret, maxRetries, backoff) = 
            agentConfig.GetDirectLineConfig(agent);

        // Create Direct Line client with agent config
        var directLineClient = new DirectLineClient(
            secret, 
            botId, 
            timeout, 
            useWebChannelSecret);

        // Get Judge configuration
        var (judgeEndpoint, judgeApiKey, judgeModel, judgeTemp, judgeTopP, judgeMaxTokens, judgeThreshold) =
            agentConfig.GetJudgeConfig(agent);

        // Create JudgeSetting object from agent config (for now, passing through)
        // In the future, JudgeSetting could be refactored to use Agent directly
        var judgeSetting = new JudgeSetting
        {
            Id = Guid.NewGuid(),
            Endpoint = judgeEndpoint,
            ApiKey = judgeApiKey,
            Model = judgeModel,
            Temperature = judgeTemp,
            TopP = judgeTopP,
            MaxOutputTokens = judgeMaxTokens,
            PassThreshold = judgeThreshold
        };

        // Create DirectLineSettings from agent config
        var directLineSettings = new DirectLineSettings
        {
            Secret = secret,
            BotId = botId,
            UseWebChannelSecret = useWebChannelSecret,
            UseWebSocket = useWebSocket,
            ReplyTimeoutSeconds = timeout,
            MaxRetries = maxRetries,
            BackoffSeconds = backoff
        };

        try
        {
            // Execute the test suite with agent-specific config
            var run = await _executionService.ExecuteTestSuiteAsync(
                suiteId,
                dbContext,
                directLineClient,
                judgeService,
                judgeSetting,
                directLineSettings,
                delayBetweenTestsMs,
                agent.Id,
                cancellationToken);

            // Link run to agent
            run.AgentId = agent.Id;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information("Test suite execution for agent {AgentName} completed. Results: {PassCount}/{TotalCount} passed",
                agent.Name, run.PassedCount, run.TotalTestCases);

            return run;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing test suite against agent {AgentName}", agent.Name);
            throw;
        }
    }

    public async Task<List<Run>> ExecuteForMultipleAgentsAsync(
        Guid suiteId,
        List<Agent> agents,
        TestRunnerDbContext dbContext,
        IJudgeService judgeService,
        IAgentConfigurationService agentConfig,
        int delayBetweenTestsMs = 2000,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Executing test suite {SuiteId} against {AgentCount} agents",
            suiteId, agents.Count);

        var runs = new List<Run>();

        foreach (var agent in agents)
        {
            try
            {
                var run = await ExecuteForAgentAsync(
                    suiteId,
                    agent,
                    dbContext,
                    judgeService,
                    agentConfig,
                    delayBetweenTestsMs,
                    cancellationToken);

                runs.Add(run);

                // Add delay between agents to prevent overload
                if (agent != agents.Last())
                {
                    var agentDelayMs = Math.Min(5000, delayBetweenTestsMs * 2);
                    _logger.Information("Waiting {DelayMs}ms before next agent execution", agentDelayMs);
                    await Task.Delay(agentDelayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to execute test suite for agent {AgentName}. Continuing with next agent...",
                    agent.Name);
            }
        }

        _logger.Information("Execution completed for {ExecutedAgentCount}/{TotalAgentCount} agents",
            runs.Count, agents.Count);

        return runs;
    }
}
