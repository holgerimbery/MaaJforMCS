using System.Text.Json;
using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Core.DirectLine;
using CopilotStudioTestRunner.Core.Evaluation;
using CopilotStudioTestRunner.Core.Execution;
using CopilotStudioTestRunner.Core.DocumentProcessing;
using CopilotStudioTestRunner.Domain.Configuration;
using CopilotStudioTestRunner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

// Setup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

var command = args[0];
var options = ParseOptions(args.Skip(1).ToArray());

return command switch
{
    "run" => await RunSuiteAsync(options),
    "list" => await ListSuitesAsync(options),
    _ => PrintUnknownCommand(command)
};

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Copilot Studio Test Runner CLI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  testrunner run --suite <name|id> [--output <dir>] [--config <path>] [--dry-run]");
    Console.WriteLine("  testrunner list [--config <path>]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --suite     Test suite name or ID to run (required for run)");
    Console.WriteLine("  --output    Output directory for results (default: ./results)");
    Console.WriteLine("  --config    Path to appsettings.json (default: appsettings.json)");
    Console.WriteLine("  --dry-run   Show what would be executed without running");
}

static Dictionary<string, string?> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        string? value = null;
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[i + 1];
            i++;
        }

        options[arg] = value;
    }

    return options;
}

static string? GetOptionValue(Dictionary<string, string?> options, string name)
{
    return options.TryGetValue(name, out var value) ? value : null;
}

static bool HasFlag(Dictionary<string, string?> options, string name)
{
    return options.ContainsKey(name);
}

static async Task<int> RunSuiteAsync(Dictionary<string, string?> options)
{
    var suite = GetOptionValue(options, "--suite");
    if (string.IsNullOrWhiteSpace(suite))
    {
        Console.Error.WriteLine("Missing required option: --suite");
        PrintUsage();
        return 1;
    }

    var output = GetOptionValue(options, "--output");
    var config = GetOptionValue(options, "--config");
    var dryRun = HasFlag(options, "--dry-run");

    try
    {
        Log.Information("Starting test run for suite: {Suite}", suite);

        var configPath = config ?? "appsettings.json";
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build();
        var testRunnerConfig = new TestRunnerConfiguration();
        configuration.Bind(testRunnerConfig);

        var dbPath = configuration.GetValue<string>("Storage:SqlitePath") ?? "./data/app.db";
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var connectionString = $"Data Source={dbPath}";

        var dbOptions = new DbContextOptionsBuilder<TestRunnerDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using var dbContext = new TestRunnerDbContext(dbOptions);
        dbContext.Database.EnsureCreated();

        var testSuite = await dbContext.TestSuites
            .Include(s => s.TestCases)
            .FirstOrDefaultAsync(s => s.Name == suite || s.Id.ToString() == suite);

        if (testSuite == null)
        {
            Log.Error("Test suite not found: {Suite}", suite);
            return 1;
        }

        Log.Information("Found suite: {SuiteName} with {TestCaseCount} test cases", testSuite.Name, testSuite.TestCases.Count);

        if (dryRun)
        {
            Console.WriteLine($"Dry run mode - would execute {testSuite.TestCases.Count} test cases");
            return 0;
        }

        var directLineClient = new DirectLineClient(
            testRunnerConfig.DirectLine.Secret,
            testRunnerConfig.DirectLine.BotId,
            testRunnerConfig.DirectLine.ReplyTimeoutSeconds);

        var judgeService = new AzureAIFoundryJudgeService();
        var executionService = new TestExecutionService();

        var judgeSettings = new JudgeSetting
        {
            Temperature = testRunnerConfig.Judge.Temperature,
            TopP = testRunnerConfig.Judge.TopP,
            MaxOutputTokens = testRunnerConfig.Judge.MaxOutputTokens,
            PassThreshold = 0.7
        };

        var run = await executionService.ExecuteTestSuiteAsync(
            testSuite.Id,
            dbContext,
            directLineClient,
            judgeService,
            judgeSettings,
            testRunnerConfig.DirectLine);

        var outputDir = output ?? "./results";
        Directory.CreateDirectory(outputDir);

        var resultsPath = Path.Combine(outputDir, $"results-{run.Id:N}.json");
        var json = JsonSerializer.Serialize(run, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(resultsPath, json);

        Log.Information("Results saved to: {Path}", resultsPath);
        Console.WriteLine("\nTest Run Summary:");
        Console.WriteLine($"  Total: {run.TotalTestCases}");
        Console.WriteLine($"  Passed: {run.PassedCount}");
        Console.WriteLine($"  Failed: {run.FailedCount}");
        Console.WriteLine($"  Pass Rate: {(100.0 * run.PassedCount / run.TotalTestCases):F1}%");
        Console.WriteLine($"  Avg Latency: {run.AverageLatencyMs:F0}ms");

        return run.FailedCount > 0 ? 1 : 0;
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Fatal error during test execution");
        return 1;
    }
}

static async Task<int> ListSuitesAsync(Dictionary<string, string?> options)
{
    var config = GetOptionValue(options, "--config");

    try
    {
        var configPath = config ?? "appsettings.json";
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build();
        var dbPath = configuration.GetValue<string>("Storage:SqlitePath") ?? "./data/app.db";
        var connectionString = $"Data Source={dbPath}";

        var dbOptions = new DbContextOptionsBuilder<TestRunnerDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using var dbContext = new TestRunnerDbContext(dbOptions);
        dbContext.Database.EnsureCreated();
        var suites = await dbContext.TestSuites.Include(s => s.TestCases).ToListAsync();

        Console.WriteLine("\nAvailable Test Suites:");
        Console.WriteLine(new string('-', 80));
        foreach (var suite in suites)
        {
            Console.WriteLine($"  {suite.Name,-30} ({suite.TestCases.Count} test cases)");
        }
        Console.WriteLine();
        return 0;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error listing test suites");
        return 1;
    }
}

