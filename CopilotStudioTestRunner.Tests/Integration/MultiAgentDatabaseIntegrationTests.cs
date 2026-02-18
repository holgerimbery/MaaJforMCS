using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CopilotStudioTestRunner.Tests.Integration;

public class MultiAgentDatabaseIntegrationTests : IAsyncLifetime
{
    private readonly DbContextOptions<TestRunnerDbContext> _dbOptions;
    private TestRunnerDbContext _dbContext = null!;

    public MultiAgentDatabaseIntegrationTests()
    {
        _dbOptions = new DbContextOptionsBuilder<TestRunnerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public async Task InitializeAsync()
    {
        _dbContext = new TestRunnerDbContext(_dbOptions);
        _dbContext.Database.EnsureCreated();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CanStoreAndRetrieveMultipleAgents()
    {
        // Arrange
        var agent1 = CreateTestAgent("Production Agent");
        var agent2 = CreateTestAgent("Staging Agent");

        // Act
        _dbContext.Agents.AddRange(agent1, agent2);
        await _dbContext.SaveChangesAsync();

        var agents = await _dbContext.Agents.ToListAsync();

        // Assert
        Assert.Equal(2, agents.Count);
    }

    [Fact]
    public async Task CanCreateRunsLinkedToAgents()
    {
        // Arrange
        var agent = CreateTestAgent("Test Agent");
        var suite = new TestSuite
        {
            Id = Guid.NewGuid(),
            Name = "Test Suite",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Agents.Add(agent);
        _dbContext.TestSuites.Add(suite);
        await _dbContext.SaveChangesAsync();

        // Act
        var run = new Run
        {
            Id = Guid.NewGuid(),
            SuiteId = suite.Id,
            AgentId = agent.Id,
            Status = "completed",
            StartedAt = DateTime.UtcNow
        };
        _dbContext.Runs.Add(run);
        await _dbContext.SaveChangesAsync();

        var savedRun = await _dbContext.Runs.FirstAsync();

        // Assert
        Assert.Equal(agent.Id, savedRun.AgentId);
        Assert.Equal(suite.Id, savedRun.SuiteId);
    }

    [Fact]
    public async Task CanFilterRunsByAgent()
    {
        // Arrange
        var agent1 = CreateTestAgent("Agent 1");
        var agent2 = CreateTestAgent("Agent 2");
        var suite = new TestSuite { Id = Guid.NewGuid(), Name = "Suite", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _dbContext.Agents.AddRange(agent1, agent2);
        _dbContext.TestSuites.Add(suite);
        await _dbContext.SaveChangesAsync();

        var run1 = new Run { Id = Guid.NewGuid(), SuiteId = suite.Id, AgentId = agent1.Id, Status = "completed", StartedAt = DateTime.UtcNow };
        var run2 = new Run { Id = Guid.NewGuid(), SuiteId = suite.Id, AgentId = agent2.Id, Status = "completed", StartedAt = DateTime.UtcNow };

        _dbContext.Runs.AddRange(run1, run2);
        await _dbContext.SaveChangesAsync();

        // Act
        var agent1Runs = await _dbContext.Runs.Where(r => r.AgentId == agent1.Id).ToListAsync();

        // Assert
        Assert.Single(agent1Runs);
        Assert.Equal(agent1.Id, agent1Runs[0].AgentId);
    }

    [Fact]
    public async Task CanQueryActiveAgents()
    {
        // Arrange
        var activeAgent = CreateTestAgent("Active");
        var inactiveAgent = CreateTestAgent("Inactive");
        inactiveAgent.IsActive = false;

        _dbContext.Agents.AddRange(activeAgent, inactiveAgent);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeAgents = await _dbContext.Agents.Where(a => a.IsActive).ToListAsync();

        // Assert
        Assert.Single(activeAgents);
    }

    private Agent CreateTestAgent(string name)
    {
        return new Agent
        {
            Id = Guid.NewGuid(),
            Name = name,
            Environment = "test",
            DirectLineBotId = $"bot-{Guid.NewGuid().ToString()[..8]}",
            DirectLineSecret = $"secret-{Guid.NewGuid().ToString()[..8]}",
            JudgeEndpoint = "https://test.com",
            JudgeApiKey = $"key-{Guid.NewGuid().ToString()[..8]}",
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
