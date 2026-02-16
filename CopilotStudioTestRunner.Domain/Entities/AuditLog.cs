namespace CopilotStudioTestRunner.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty; // 'Create', 'Update', 'Delete', 'Run'
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? Details { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
