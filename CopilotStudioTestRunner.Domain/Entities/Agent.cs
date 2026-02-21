namespace CopilotStudioTestRunner.Domain.Entities;

public class Agent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = "production"; // 'dev', 'test', 'staging', 'production'
    public string[] Tags { get; set; } = [];
    
    // Direct Line Configuration
    public string DirectLineBotId { get; set; } = string.Empty;
    public string DirectLineSecret { get; set; } = string.Empty;
    public bool DirectLineUseWebChannelSecret { get; set; } = false;
    public bool DirectLineUseWebSocket { get; set; } = true;
    public int DirectLineReplyTimeoutSeconds { get; set; } = 30;
    public int DirectLineMaxRetries { get; set; } = 2;
    public int DirectLineBackoffSeconds { get; set; } = 4;
    
    // Judge Configuration
    public string JudgeEndpoint { get; set; } = string.Empty;
    public string JudgeApiKey { get; set; } = string.Empty;
    public string JudgeModel { get; set; } = "gpt-4o-mini";
    public double JudgeTemperature { get; set; } = 0.2;
    public double JudgeTopP { get; set; } = 0.9;
    public double JudgePassThreshold { get; set; } = 0.7;
    public int JudgeMaxOutputTokens { get; set; } = 800;
    
    // Optional: Override Question Generation (null = use global settings)
    public string? QuestionGenEndpoint { get; set; }
    public string? QuestionGenApiKey { get; set; }
    public string? QuestionGenModel { get; set; }
    public string? QuestionGenSystemPrompt { get; set; }
    
    // Metadata
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation
    public ICollection<TestSuiteAgent> TestSuiteAgents { get; set; } = [];
    public ICollection<Run> Runs { get; set; } = [];
}
