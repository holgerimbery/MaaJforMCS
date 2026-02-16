namespace CopilotStudioTestRunner.Domain.Configuration;

public class DirectLineSettings
{
    public string Secret { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public bool UseWebChannelSecret { get; set; } = false;
    public bool UseWebSocket { get; set; } = true;
    public int ReplyTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 2;
    public int BackoffSeconds { get; set; } = 4;
}

public class JudgeSettings
{
    public string Provider { get; set; } = "AzureAIFoundry";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 0.9;
    public int MaxOutputTokens { get; set; } = 800;
}

public class ExecutionSettings
{
    public int MaxConcurrency { get; set; } = 5;
    public int RateLimitPerMinute { get; set; } = 50;
    public int Retries { get; set; } = 2;
    public int BackoffSeconds { get; set; } = 4;
}

public class StorageSettings
{
    public string SqlitePath { get; set; } = "./data/app.db";
    public string LuceneIndexPath { get; set; } = "./data/index";
    public string DocumentUploadPath { get; set; } = "./data/uploads";
}

public class TestRunnerConfiguration
{
    public DirectLineSettings DirectLine { get; set; } = new();
    public JudgeSettings Judge { get; set; } = new();
    public ExecutionSettings Execution { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
}
