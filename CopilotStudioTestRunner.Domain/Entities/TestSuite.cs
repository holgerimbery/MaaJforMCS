namespace CopilotStudioTestRunner.Domain.Entities;

public class TestSuite
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    
    // Configuration
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 2;
    
    // Relationships
    public ICollection<TestCase> TestCases { get; set; } = [];
    public ICollection<Run> Runs { get; set; } = [];
}
