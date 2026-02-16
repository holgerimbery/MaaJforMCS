namespace CopilotStudioTestRunner.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Run
{
    public Guid Id { get; set; }
    public Guid SuiteId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "running"; // 'running', 'completed', 'failed'
    
    // Execution context
    public string? ExecutionUser { get; set; }
    public string? GitSha { get; set; }
    public string? ModelVersion { get; set; }
    public string? PromptVersion { get; set; }
    
    [NotMapped]
    public Dictionary<string, object>? ConfigSnapshot { get; set; }
    
    // Summary stats
    public int TotalTestCases { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MedianLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    
    // Navigation
    public TestSuite? Suite { get; set; }
    public ICollection<Result> Results { get; set; } = [];
}
