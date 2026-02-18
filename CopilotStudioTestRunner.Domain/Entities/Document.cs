namespace CopilotStudioTestRunner.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "text"; // 'pdf' or 'text'
    public string ContentHash { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string? StoragePath { get; set; }
    
    // Relationships
    public ICollection<Chunk> Chunks { get; set; } = [];
    public ICollection<TestCase> GeneratedTestCases { get; set; } = [];
    public ICollection<TestCaseDocument> TestCaseDocuments { get; set; } = [];
}
