namespace CopilotStudioTestRunner.Domain.Entities;

public class TestCase
{
    public Guid Id { get; set; }
    public Guid SuiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Test definition
    public string[] UserInput { get; set; } = []; // Multi-turn conversation steps
    public string? ExpectedIntent { get; set; }
    public string[] ExpectedEntities { get; set; } = [];
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string? ReferenceAnswer { get; set; }
    
    // Metadata
    public string Category { get; set; } = "general";
    public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsGenerated { get; set; } = false;
    
    // Timing
    public int? TimeoutSeconds { get; set; }
    public int? MaxRetries { get; set; }
    
    // Execution tracking
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? SourceDocumentId { get; set; } // If generated from a document
    
    // Navigation
    public TestSuite? Suite { get; set; }
    public Document? SourceDocument { get; set; }
    public ICollection<Result> Results { get; set; } = [];
}
