namespace CopilotStudioTestRunner.Domain.Entities;

public class TranscriptMessage
{
    public Guid Id { get; set; }
    public Guid ResultId { get; set; }
    public string Role { get; set; } = string.Empty; // 'user', 'bot', 'system'
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int SequenceNumber { get; set; }
    public string? RawActivityJson { get; set; } // Store full Direct Line activity
    
    // Navigation
    public Result? Result { get; set; }
}
