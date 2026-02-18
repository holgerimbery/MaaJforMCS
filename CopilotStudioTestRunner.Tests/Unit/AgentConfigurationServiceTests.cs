using CopilotStudioTestRunner.Core.Services;
using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CopilotStudioTestRunner.Tests.Unit;

public class AgentConfigurationServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<TestRunnerDbContext> _dbOptions;
    private TestRunnerDbContext _dbContext = null!;
    private IAgentConfigurationService _service = null!;

    public AgentConfigurationServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<TestRunnerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public async Task InitializeAsync()
    {
        _dbContext = new TestRunnerDbContext(_dbOptions);
        _dbContext.Database.EnsureCreated();
        _service = new AgentConfigurationService();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetDirectLineConfig_WithValidAgent_ReturnsAgentDirectLineSettings()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            DirectLineBotId = "bot-123",
            DirectLineSecret = "secret-456",
            DirectLineReplyTimeoutSeconds = 30,
            DirectLineUseWebChannelSecret = true,
            DirectLineUseWebSocket = false,
            DirectLineMaxRetries = 2,
            DirectLineBackoffSeconds = 5,
            Environment = "test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var (botId, secret, timeoutSeconds, useWebSocket, useWebChannelSecret, maxRetries, backoffSeconds) = _service.GetDirectLineConfig(agent);

        // Assert
        Assert.Equal("bot-123", botId);
        Assert.Equal("secret-456", secret);
        Assert.Equal(30, timeoutSeconds);
        Assert.True(useWebChannelSecret);
        Assert.False(useWebSocket);
        Assert.Equal(2, maxRetries);
        Assert.Equal(5, backoffSeconds);
    }

    [Fact]
    public void GetDirectLineConfig_WithNullAgent_ThrowsNullReferenceException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _service.GetDirectLineConfig(null!));
    }

    [Fact]
    public async Task GetJudgeConfig_WithValidAgent_ReturnsAgentJudgeSettings()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            DirectLineBotId = "bot-123",
            DirectLineSecret = "secret-456",
            JudgeEndpoint = "https://api.example.com",
            JudgeApiKey = "judge-key-789",
            JudgeModel = "gpt-4o",
            JudgeTemperature = 0.5,
            JudgeTopP = 0.9,
            JudgePassThreshold = 0.75,
            JudgeMaxOutputTokens = 800,
            Environment = "test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var (endpoint, apiKey, model, temperature, topP, maxOutputTokens, passThreshold) = _service.GetJudgeConfig(agent);

        // Assert
        Assert.Equal("https://api.example.com", endpoint);
        Assert.Equal("judge-key-789", apiKey);
        Assert.Equal("gpt-4o", model);
        Assert.Equal(0.5, temperature);
        Assert.Equal(0.9, topP);
        Assert.Equal(0.75, passThreshold);
        Assert.Equal(800, maxOutputTokens);
    }

    [Fact]
    public void GetJudgeConfig_WithNullAgent_ThrowsNullReferenceException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _service.GetJudgeConfig(null!));
    }

    [Fact]
    public async Task GetQuestionGenerationConfigAsync_WhenNoGlobalSettingAndNoAgent_CreatesGlobalAndReturnsDefault()
    {
        // Act
        var (endpoint, apiKey, model) = await _service.GetQuestionGenerationConfigAsync(null, _dbContext);

        // Assert
        Assert.NotNull(endpoint);
        Assert.NotNull(apiKey);
        Assert.NotNull(model);
        
        // Verify setting was created
        var savedSetting = await _dbContext.GlobalQuestionGenerationSettings.FirstOrDefaultAsync();
        Assert.NotNull(savedSetting);
    }

    [Fact]
    public async Task GetQuestionGenerationConfigAsync_WhenGlobalSettingExists_ReturnsExistingSetting()
    {
        // Arrange
        var existingSetting = new GlobalQuestionGenerationSetting
        {
            Id = Guid.NewGuid(),
            Endpoint = "https://existing.example.com",
            ApiKey = "existing-key",
            Model = "existing-model",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _dbContext.GlobalQuestionGenerationSettings.Add(existingSetting);
        await _dbContext.SaveChangesAsync();

        // Act
        var (endpoint, apiKey, model) = await _service.GetQuestionGenerationConfigAsync(null, _dbContext);

        // Assert
        Assert.Equal("https://existing.example.com", endpoint);
        Assert.Equal("existing-key", apiKey);
        Assert.Equal("existing-model", model);
        
        // Verify no additional settings created
        var settingsCount = await _dbContext.GlobalQuestionGenerationSettings.CountAsync();
        Assert.Equal(1, settingsCount);
    }

    [Fact]
    public async Task GetQuestionGenerationConfigAsync_WithAgentOverrides_ReturnsAgentConfig()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Agent with QGen Override",
            DirectLineBotId = "bot-123",
            DirectLineSecret = "secret-123",
            QuestionGenEndpoint = "https://agent-qgen.example.com",
            QuestionGenApiKey = "agent-qgen-key",
            QuestionGenModel = "agent-qgen-model",
            Environment = "test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var (endpoint, apiKey, model) = await _service.GetQuestionGenerationConfigAsync(agent, _dbContext);

        // Assert
        Assert.Equal("https://agent-qgen.example.com", endpoint);
        Assert.Equal("agent-qgen-key", apiKey);
        Assert.Equal("agent-qgen-model", model);
    }

    [Fact]
    public async Task GetOrCreateGlobalQuestionGenSettingAsync_WhenNoSettingExists_CreatesNew()
    {
        // Act
        var setting = await _service.GetOrCreateGlobalQuestionGenSettingAsync(_dbContext);

        // Assert
        Assert.NotNull(setting);
        var savedSetting = await _dbContext.GlobalQuestionGenerationSettings.FirstOrDefaultAsync();
        Assert.NotNull(savedSetting);
        Assert.Equal(setting.Id, savedSetting.Id);
    }

    [Fact]
    public async Task GetOrCreateGlobalQuestionGenSettingAsync_WhenSettingExists_ReturnsExisting()
    {
        // Arrange
        var existingSetting = new GlobalQuestionGenerationSetting
        {
            Id = Guid.NewGuid(),
            Endpoint = "https://fixed.example.com",
            ApiKey = "fixed-key",
            Model = "fixed-model",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _dbContext.GlobalQuestionGenerationSettings.Add(existingSetting);
        await _dbContext.SaveChangesAsync();

        // Act
        var setting = await _service.GetOrCreateGlobalQuestionGenSettingAsync(_dbContext);

        // Assert
        Assert.NotNull(setting);
        Assert.Equal(existingSetting.Id, setting.Id);
        
        // Verify no additional settings created
        var settingsCount = await _dbContext.GlobalQuestionGenerationSettings.CountAsync();
        Assert.Equal(1, settingsCount);
    }

    [Fact]
    public async Task GetJudgeConfig_WithMultipleAgents_ReturnsDifferentConfigsPerAgent()
    {
        // Arrange
        var agent1 = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Agent 1",
            DirectLineBotId = "bot-1",
            DirectLineSecret = "secret-1",
            JudgeEndpoint = "https://api1.example.com",
            JudgeApiKey = "key-1",
            JudgeModel = "gpt-4o",
            JudgePassThreshold = 0.7,
            JudgeTemperature = 0.2,
            JudgeTopP = 0.9,
            JudgeMaxOutputTokens = 800,
            Environment = "prod",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var agent2 = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Agent 2",
            DirectLineBotId = "bot-2",
            DirectLineSecret = "secret-2",
            JudgeEndpoint = "https://api2.example.com",
            JudgeApiKey = "key-2",
            JudgeModel = "gpt-4-turbo",
            JudgePassThreshold = 0.8,
            JudgeTemperature = 0.3,
            JudgeTopP = 0.95,
            JudgeMaxOutputTokens = 1000,
            Environment = "staging",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var (endpoint1, apiKey1, model1, temp1, topP1, maxOut1, threshold1) = _service.GetJudgeConfig(agent1);
        var (endpoint2, apiKey2, model2, temp2, topP2, maxOut2, threshold2) = _service.GetJudgeConfig(agent2);

        // Assert
        Assert.NotEqual(endpoint1, endpoint2);
        Assert.NotEqual(apiKey1, apiKey2);
        Assert.NotEqual(model1, model2);
        Assert.NotEqual(threshold1, threshold2);
        Assert.NotEqual(temp1, temp2);
        Assert.NotEqual(topP1, topP2);
        Assert.NotEqual(maxOut1, maxOut2);
    }
}
