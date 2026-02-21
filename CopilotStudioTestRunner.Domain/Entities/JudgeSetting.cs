namespace CopilotStudioTestRunner.Domain.Entities;

public class JudgeSetting
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Scoring weights (sum should be 1.0)
    public double TaskSuccessWeight { get; set; } = 0.3;
    public double IntentMatchWeight { get; set; } = 0.2;
    public double FactualityWeight { get; set; } = 0.2;
    public double HelpfulnessWeight { get; set; } = 0.15;
    public double SafetyWeight { get; set; } = 0.15;
    
    // Thresholds
    public double PassThreshold { get; set; } = 0.7;
    public bool UseReferenceAnswer { get; set; } = false;
    
    // Model parameters — null means "inherit from the agent's judge LLM"
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 0.9;
    public int MaxOutputTokens { get; set; } = 800;
    
    // Azure AI Foundry settings — null/empty means "inherit from the agent's judge LLM"
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    
    public bool IsDefault { get; set; } = false;
}
