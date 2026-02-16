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
}
