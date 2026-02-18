using CopilotStudioTestRunner.Core.Services;
using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CopilotStudioTestRunner.Tests.EndToEnd;

public class MultiAgentEndToEndTests : IAsyncLifetime
{
    private readonly DbContextOptions<TestRunnerDbContext> _dbOptions;
    private TestRunnerDbContext _dbContext = null!;
    private IAgentConfigurationService _agentConfigService = null!;

    public MultiAgentEndToEndTests()
    {
        _dbOptions = new DbContextOptionsBuilder<TestRunnerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public async Task InitializeAsync()
    {
        _dbContext = new TestRunnerDbContext(_dbOptions);
        _dbContext.Database.EnsureCreated();
        _agentConfigService = new AgentConfigurationService();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task E2E_CanCreateAgentAndTestSuite()
    {
        // Step 1: Create agent
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "E2E Test Agent",
            Environment = "test",
            DirectLineBotId = "e2e-bot",
            DirectLineSecret = "e2e-secret",
            DirectLineReplyTimeoutSeconds = 30,
            DirectLineUseWebChannelSecret = false,
            DirectLineUseWebSocket = true,
            DirectLineMaxRetries = 2,
            DirectLineBackoffSeconds = 3,
            JudgeEndpoint = "https://api.e2e.com",
            JudgeApiKey = "e2e-judge-key",
            JudgeModel = "gpt-4o",
            JudgeTemperature = 0.2,
            JudgeTopP = 0.9,
            JudgePassThreshold = 0.7,
            JudgeMaxOutputTokens = 800,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        // Step 2: Create test suite
        var suite = new TestSuite
        {
            Id = Guid.NewGuid(),
            Name = "E2E Test Suite",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TestSuites.Add(suite);
        await _dbContext.SaveChangesAsync();

        // Step 3: Create test cases
        var testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            SuiteId = suite.Id,
            Name = "E2E Test Case",
            UserInput = new[] { "test input" },
            ExpectedIntent = "test",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TestCases.Add(testCase);
        await _dbContext.SaveChangesAsync();

        // Step 4: Verify agent configuration retrieval
        var (botId, secret, _, _, _, _, _) = _agentConfigService.GetDirectLineConfig(agent);

        // Assert
        Assert.Equal("e2e-bot", botId);
        Assert.Equal("e2e-secret", secret);

        var suites = await _dbContext.TestSuites.Include(s => s.TestCases).ToListAsync();
        Assert.Single(suites);
        Assert.Single(suites[0].TestCases);
    }

    [Fact]
    public async Task E2E_CanExecuteTestWithAgent()
    {
        // Arrange: Create agent, suite, and run
        var agent = CreateAgent("E2E Agent");
        var suite = new TestSuite { Id = Guid.NewGuid(), Name = "E2E Suite", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _dbContext.Agents.Add(agent);
        _dbContext.TestSuites.Add(suite);
        await _dbContext.SaveChangesAsync();

        // Act: Create and save a run
        var run = new Run
        {
            Id = Guid.NewGuid(),
            SuiteId = suite.Id,
            AgentId = agent.Id,
            Status = "completed",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddMinutes(1),
            TotalTestCases = 1,
            PassedCount = 1,
            FailedCount = 0
        };

        _dbContext.Runs.Add(run);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedRun = await _dbContext.Runs
            .Include(r => r.Suite)
            .FirstAsync(r => r.Id == run.Id);

        Assert.Equal(agent.Id, savedRun.AgentId);
        Assert.Equal(suite.Id, savedRun.SuiteId);
        Assert.Equal(1, savedRun.PassedCount);
    }

    [Fact]
    public async Task E2E_MultiAgentExecution()
    {
        // Arrange: Create multiple agents
        var agent1 = CreateAgent("Agent 1");
        var agent2 = CreateAgent("Agent 2");
        var suite = new TestSuite { Id = Guid.NewGuid(), Name = "Multi Suite", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _dbContext.Agents.AddRange(agent1, agent2);
        _dbContext.TestSuites.Add(suite);
        await _dbContext.SaveChangesAsync();

        // Act: Create runs for both agents
        var runs = new[]
        {
            new Run { Id = Guid.NewGuid(), SuiteId = suite.Id, AgentId = agent1.Id, Status = "completed", StartedAt = DateTime.UtcNow, PassedCount = 5 },
            new Run { Id = Guid.NewGuid(), SuiteId = suite.Id, AgentId = agent2.Id, Status = "completed", StartedAt = DateTime.UtcNow, PassedCount = 4 }
        };

        _dbContext.Runs.AddRange(runs);
        await _dbContext.SaveChangesAsync();

        // Assert: Verify both agents have runs
        var agent1Runs = await _dbContext.Runs.Where(r => r.AgentId == agent1.Id).ToListAsync();
        var agent2Runs = await _dbContext.Runs.Where(r => r.AgentId == agent2.Id).ToListAsync();

        Assert.Single(agent1Runs);
        Assert.Single(agent2Runs);
        Assert.Equal(5, agent1Runs[0].PassedCount);
        Assert.Equal(4, agent2Runs[0].PassedCount);
    }

    private Agent CreateAgent(string name)
    {
        return new Agent
        {
            Id = Guid.NewGuid(),
            Name = name,
            Environment = "test",
            DirectLineBotId = $"bot-{Guid.NewGuid().ToString()[..6]}",
            DirectLineSecret = $"secret-{Guid.NewGuid().ToString()[..6]}",
            JudgeEndpoint = "https://test.com",
            JudgeApiKey = $"key-{Guid.NewGuid().ToString()[..6]}",
            JudgeModel = "gpt-4o",
            JudgePassThreshold = 0.7,
            JudgeTemperature = 0.2,
            JudgeTopP = 0.9,
            JudgeMaxOutputTokens = 800,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
