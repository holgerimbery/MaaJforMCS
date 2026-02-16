namespace CopilotStudioTestRunner.Domain.Entities;

public class Result
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid TestCaseId { get; set; }
    public string Verdict { get; set; } = "unknown"; // 'pass', 'fail', 'error', 'skipped'
    
    // Scoring (0.0 to 1.0)
    public double? TaskSuccessScore { get; set; }
    public double? IntentMatchScore { get; set; }
    public double? FactualityScore { get; set; }
    public double? HelpfulnessScore { get; set; }
    public double? SafetyScore { get; set; }
    public double? OverallScore { get; set; }
    
    // Execution metrics
    public long LatencyMs { get; set; }
    public int TurnCount { get; set; }
    public int? TokensUsed { get; set; }
    
    // Judge feedback
    public string? JudgeRationale { get; set; }
    public string[]? JudgeCitations { get; set; }
    
    // Error/Notes
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
    
    // Navigation
    public Run? Run { get; set; }
    public TestCase? TestCase { get; set; }
    public ICollection<TranscriptMessage> TranscriptMessages { get; set; } = [];
}
