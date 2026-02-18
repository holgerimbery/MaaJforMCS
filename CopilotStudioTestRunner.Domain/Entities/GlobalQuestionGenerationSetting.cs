namespace CopilotStudioTestRunner.Domain.Entities;

public class GlobalQuestionGenerationSetting
{
    public Guid Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
    public int MaxOutputTokens { get; set; } = 1000;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
