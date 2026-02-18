namespace CopilotStudioTestRunner.Domain.Entities;

/// <summary>
/// Many-to-many join table: TestSuites can run against multiple Agents
/// </summary>
public class TestSuiteAgent
{
    public Guid TestSuiteId { get; set; }
    public TestSuite TestSuite { get; set; } = null!;
    
    public Guid AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
}
