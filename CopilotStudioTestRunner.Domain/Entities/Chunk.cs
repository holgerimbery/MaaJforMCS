namespace CopilotStudioTestRunner.Domain.Entities;

public class Chunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public int ChunkIndex { get; set; }
    public double? StartChapter { get; set; }
    public double? EndChapter { get; set; }
    public string? Category { get; set; }
    public byte[]? Embedding { get; set; } // Optional: for semantic search
    
    // Navigation
    public Document? Document { get; set; }
}
