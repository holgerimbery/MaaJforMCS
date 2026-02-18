using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CopilotStudioTestRunner.Core.Execution;

/// <summary>
/// Seeds test data for multi-agent testing scenarios
/// </summary>
public class TestDataSeeder
{
    private readonly TestRunnerDbContext _dbContext;
    private readonly ILogger _logger = Log.ForContext<TestDataSeeder>();

    public TestDataSeeder(TestRunnerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Seeds sample agents and test data for testing multi-agent scenarios
    /// </summary>
    public async Task SeedSampleDataAsync()
    {
        _logger.Information("Starting test data seeding");

        try
        {
            // Create sample agents
            var agent1 = new Agent
            {
                Id = Guid.NewGuid(),
                Name = "Production Agent - US",
                Description = "Production agent deployed in US region",
                Environment = "production",
                DirectLineBotId = "prod-us-bot-001",
                DirectLineSecret = "test-secret-prod-us-12345678",
                DirectLineUseWebChannelSecret = false,
                DirectLineUseWebSocket = true,
                DirectLineReplyTimeoutSeconds = 30,
                DirectLineMaxRetries = 2,
                DirectLineBackoffSeconds = 4,
                JudgeEndpoint = "https://api.openai.com/v1",
                JudgeApiKey = "sk-test-prod-us-judge-key",
                JudgeModel = "gpt-4o",
                JudgeTemperature = 0.2,
                JudgeTopP = 0.9,
                JudgePassThreshold = 0.75,
                JudgeMaxOutputTokens = 800,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "seeder"
            };

            var agent2 = new Agent
            {
                Id = Guid.NewGuid(),
                Name = "Staging Agent - EU",
                Description = "Staging agent deployed in EU region",
                Environment = "staging",
                DirectLineBotId = "stage-eu-bot-001",
                DirectLineSecret = "test-secret-stage-eu-87654321",
                DirectLineUseWebChannelSecret = false,
                DirectLineUseWebSocket = true,
                DirectLineReplyTimeoutSeconds = 25,
                DirectLineMaxRetries = 3,
                DirectLineBackoffSeconds = 5,
                JudgeEndpoint = "https://api.openai.com/v1",
                JudgeApiKey = "sk-test-stage-eu-judge-key",
                JudgeModel = "gpt-4o-mini",
                JudgeTemperature = 0.15,
                JudgeTopP = 0.85,
                JudgePassThreshold = 0.70,
                JudgeMaxOutputTokens = 600,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-1),
                CreatedBy = "seeder"
            };

            var agent3 = new Agent
            {
                Id = Guid.NewGuid(),
                Name = "Test Agent - Local Dev",
                Description = "Local development agent for testing",
                Environment = "dev",
                DirectLineBotId = "dev-local-bot-001",
                DirectLineSecret = "test-secret-dev-local-11111111",
                DirectLineUseWebChannelSecret = false,
                DirectLineUseWebSocket = false,
                DirectLineReplyTimeoutSeconds = 15,
                DirectLineMaxRetries = 1,
                DirectLineBackoffSeconds = 2,
                JudgeEndpoint = "http://localhost:8000",
                JudgeApiKey = "dev-local-judge-key",
                JudgeModel = "local-model",
                JudgeTemperature = 0.3,
                JudgeTopP = 1.0,
                JudgePassThreshold = 0.65,
                JudgeMaxOutputTokens = 500,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-2),
                CreatedBy = "seeder"
            };

            // Check if agents already exist
            var existingAgents = await _dbContext.Agents.CountAsync();
            if (existingAgents == 0)
            {
                _dbContext.Agents.AddRange(agent1, agent2, agent3);
                await _dbContext.SaveChangesAsync();
                _logger.Information("Created {AgentCount} test agents", 3);
            }
            else
            {
                _logger.Information("Agents already exist, skipping agent creation");
            }

            // Create sample test suite
            var existingSuites = await _dbContext.TestSuites.CountAsync();
            if (existingSuites == 0)
            {
                var testSuite = new TestSuite
                {
                    Id = Guid.NewGuid(),
                    Name = "Multi-Agent Test Suite",
                    Description = "Test suite for validating multi-agent execution",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "seeder"
                };

                _dbContext.TestSuites.Add(testSuite);
                await _dbContext.SaveChangesAsync();

                // Create test cases
                var testCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Id = Guid.NewGuid(),
                        SuiteId = testSuite.Id,
                        Name = "Basic Greeting Test",
                        Description = "Test basic greeting functionality",
                        UserInput = new[] { "Hello, how are you?", "Hi there!" },
                        ExpectedIntent = "greeting",
                        AcceptanceCriteria = "Bot should respond with a friendly greeting",
                        Priority = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new TestCase
                    {
                        Id = Guid.NewGuid(),
                        SuiteId = testSuite.Id,
                        Name = "Help Request Test",
                        Description = "Test help command functionality",
                        UserInput = new[] { "Can you help me?", "I need help" },
                        ExpectedIntent = "help",
                        AcceptanceCriteria = "Bot should provide helpful information",
                        Priority = 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new TestCase
                    {
                        Id = Guid.NewGuid(),
                        SuiteId = testSuite.Id,
                        Name = "Information Query Test",
                        Description = "Test information retrieval",
                        UserInput = new[] { "What is your name?", "Tell me about yourself" },
                        ExpectedIntent = "info_query",
                        AcceptanceCriteria = "Bot should return accurate information",
                        Priority = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _dbContext.TestCases.AddRange(testCases);
                await _dbContext.SaveChangesAsync();

                _logger.Information("Created test suite and {TestCaseCount} test cases", testCases.Count);
            }
            else
            {
                _logger.Information("Test suites already exist, skipping suite creation");
            }

            // Create sample runs (simulated execution results)
            var existingRuns = await _dbContext.Runs.CountAsync();
            if (existingRuns == 0)
            {
                var agentIds = await _dbContext.Agents.Select(a => a.Id).Take(3).ToListAsync();
                var suiteIds = await _dbContext.TestSuites.Select(s => s.Id).Take(1).ToListAsync();

                if (agentIds.Count > 0 && suiteIds.Count > 0)
                {
                    var sampleRuns = new List<Run>();

                    foreach (var agentIdValue in agentIds)
                    {
                        for (int i = 1; i <= 2; i++)
                        {
                            var run = new Run
                            {
                                Id = Guid.NewGuid(),
                                SuiteId = suiteIds[0],
                                AgentId = agentIdValue,
                                StartedAt = DateTime.UtcNow.AddHours(-i * 24),
                                CompletedAt = DateTime.UtcNow.AddHours(-i * 24).AddMinutes(5),
                                Status = "completed",
                                TotalTestCases = 3,
                                PassedCount = 2 + i % 2,
                                FailedCount = 1 - (i % 2),
                                SkippedCount = 0,
                                AverageLatencyMs = 250 + (RandomValue() % 200),
                                MedianLatencyMs = 240 + (RandomValue() % 180),
                                P95LatencyMs = 400 + (RandomValue() % 300)
                            };

                            sampleRuns.Add(run);
                        }
                    }

                    _dbContext.Runs.AddRange(sampleRuns);
                    await _dbContext.SaveChangesAsync();

                    _logger.Information("Created {RunCount} sample runs", sampleRuns.Count);
                }
            }
            else
            {
                _logger.Information("Runs already exist, skipping run creation");
            }

            _logger.Information("Test data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error seeding test data");
            throw;
        }
    }

    private static int RandomValue()
    {
        return Random.Shared.Next(1, 1000);
    }
}
