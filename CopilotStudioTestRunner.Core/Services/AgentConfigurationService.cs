using CopilotStudioTestRunner.Domain.Entities;
using CopilotStudioTestRunner.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CopilotStudioTestRunner.Core.Services;

/// <summary>
/// Service for managing and retrieving agent-based configurations
/// Handles fallback from agent-specific overrides to global defaults
/// </summary>
public interface IAgentConfigurationService
{
    /// <summary>
    /// Get Direct Line configuration for an agent
    /// </summary>
    (string BotId, string Secret, int TimeoutSeconds, bool UseWebSocket, bool UseWebChannelSecret, int MaxRetries, int BackoffSeconds) 
        GetDirectLineConfig(Agent agent);

    /// <summary>
    /// Get Judge configuration for an agent
    /// </summary>
    (string Endpoint, string ApiKey, string Model, double Temperature, double TopP, int MaxOutputTokens, double PassThreshold) 
        GetJudgeConfig(Agent agent);

    /// <summary>
    /// Get Question Generation configuration for an agent, with fallback to global
    /// </summary>
    Task<(string Endpoint, string ApiKey, string Model)> GetQuestionGenerationConfigAsync(
        Agent? agent,
        TestRunnerDbContext dbContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create global question generation settings
    /// </summary>
    Task<GlobalQuestionGenerationSetting> GetOrCreateGlobalQuestionGenSettingAsync(
        TestRunnerDbContext dbContext,
        CancellationToken cancellationToken = default);
}

public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly ILogger _logger = Log.ForContext<AgentConfigurationService>();

    public (string BotId, string Secret, int TimeoutSeconds, bool UseWebSocket, bool UseWebChannelSecret, int MaxRetries, int BackoffSeconds)
        GetDirectLineConfig(Agent agent)
    {
        return (
            agent.DirectLineBotId,
            agent.DirectLineSecret,
            agent.DirectLineReplyTimeoutSeconds,
            agent.DirectLineUseWebSocket,
            agent.DirectLineUseWebChannelSecret,
            agent.DirectLineMaxRetries,
            agent.DirectLineBackoffSeconds
        );
    }

    public (string Endpoint, string ApiKey, string Model, double Temperature, double TopP, int MaxOutputTokens, double PassThreshold)
        GetJudgeConfig(Agent agent)
    {
        return (
            agent.JudgeEndpoint,
            agent.JudgeApiKey,
            agent.JudgeModel,
            agent.JudgeTemperature,
            agent.JudgeTopP,
            agent.JudgeMaxOutputTokens,
            agent.JudgePassThreshold
        );
    }

    public async Task<(string Endpoint, string ApiKey, string Model)> GetQuestionGenerationConfigAsync(
        Agent? agent,
        TestRunnerDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        // Check if agent has overridden settings
        if (agent != null && 
            !string.IsNullOrEmpty(agent.QuestionGenEndpoint) &&
            !string.IsNullOrEmpty(agent.QuestionGenApiKey) &&
            !string.IsNullOrEmpty(agent.QuestionGenModel))
        {
            _logger.Information("Using agent-specific question generation config for agent: {AgentName}", agent.Name);
            return (agent.QuestionGenEndpoint, agent.QuestionGenApiKey, agent.QuestionGenModel);
        }

        // Fall back to global settings
        var globalSettings = await GetOrCreateGlobalQuestionGenSettingAsync(dbContext, cancellationToken);
        _logger.Information("Using global question generation config");
        return (globalSettings.Endpoint, globalSettings.ApiKey, globalSettings.Model);
    }

    public async Task<GlobalQuestionGenerationSetting> GetOrCreateGlobalQuestionGenSettingAsync(
        TestRunnerDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.GlobalQuestionGenerationSettings.FirstOrDefaultAsync(cancellationToken);
        
        if (existing != null)
        {
            return existing;
        }

        // Create default if none exists
        _logger.Warning("No global question generation settings found, creating default placeholder");
        var defaultSettings = new GlobalQuestionGenerationSetting
        {
            Id = Guid.NewGuid(),
            Endpoint = string.Empty,
            ApiKey = string.Empty,
            Model = "gpt-4o-mini",
            Temperature = 0.7,
            TopP = 1.0,
            MaxOutputTokens = 1000,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "system"
        };

        dbContext.GlobalQuestionGenerationSettings.Add(defaultSettings);
        await dbContext.SaveChangesAsync(cancellationToken);

        return defaultSettings;
    }
}
