namespace CopilotStudioTestRunner.Domain.Entities;

public class TestCaseDocument
{
    public Guid TestCaseId { get; set; }
    public Guid DocumentId { get; set; }
    public DateTime CreatedAt { get; set; }

    public TestCase? TestCase { get; set; }
    public Document? Document { get; set; }
}
