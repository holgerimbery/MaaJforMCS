namespace CopilotStudioTestRunner.WebUI.Api;

public class CreateTestSuiteRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateTestSuiteRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class StartRunRequest
{
    public Guid SuiteId { get; set; }
    public List<Guid>? SelectedTestCaseIds { get; set; }
    public Dictionary<string, object>? Overrides { get; set; }
    /// <summary>
    /// Optional: specific agent IDs to run against. If null, runs against agents linked to the suite.
    /// </summary>
    public List<Guid>? AgentIds { get; set; }
}
