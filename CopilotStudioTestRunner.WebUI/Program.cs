using System.Security.Claims;
using CopilotStudioTestRunner.Data;
using CopilotStudioTestRunner.Domain.Entities;
using CopilotStudioTestRunner.Domain.Configuration;
using CopilotStudioTestRunner.Core.DirectLine;
using CopilotStudioTestRunner.Core.Evaluation;
using CopilotStudioTestRunner.Core.Execution;
using CopilotStudioTestRunner.Core.DocumentProcessing;
using CopilotStudioTestRunner.Core.Services;
using CopilotStudioTestRunner.WebUI.Api;
using CopilotStudioTestRunner.WebUI.Authentication;
using CopilotStudioTestRunner.WebUI.Components;
using CopilotStudioTestRunner.WebUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Load configuration
var config = builder.Configuration;

// Ensure data directory exists
var storageConfig = new { SqlitePath = config.GetValue<string>("Storage:SqlitePath") ?? "./data/app.db" };
var dbPath = storageConfig.SqlitePath;
var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
{
    Directory.CreateDirectory(dbDir);
}

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
var connectionString = $"Data Source={dbPath}";
builder.Services.AddDbContext<TestRunnerDbContext>(options =>
    options.UseSqlite(connectionString));

// Configuration
var testRunnerConfig = new TestRunnerConfiguration();
config.Bind(testRunnerConfig);
builder.Services.AddSingleton(testRunnerConfig);

// Core services
builder.Services.AddScoped<DirectLineClient>(sp =>
{
    var dlSettings = testRunnerConfig.DirectLine;
    return new DirectLineClient(dlSettings.Secret, dlSettings.BotId, dlSettings.ReplyTimeoutSeconds, dlSettings.UseWebChannelSecret);
});

builder.Services.AddScoped<IJudgeService, AzureAIFoundryJudgeService>();
builder.Services.AddScoped<IQuestionGenerationService, AzureOpenAIQuestionGenerationService>();
builder.Services.AddScoped<ITestExecutionService, TestExecutionService>();
builder.Services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();
builder.Services.AddScoped<IMultiAgentExecutionCoordinator, MultiAgentExecutionCoordinator>();
builder.Services.AddScoped<IDocumentIngestor, DocumentIngestor>();
builder.Services.AddScoped<IDocumentChunker, DocumentChunker>();
builder.Services.AddScoped<IPowerPlatformDiscoveryService, PowerPlatformDiscoveryService>();

// HTTP client
builder.Services.AddHttpClient();

// Add health checks
builder.Services.AddHealthChecks();

// ── Authentication & Authorization ──────────────────────────────
var authEnabled = config.GetValue<bool>("Authentication:Enabled", false);

if (authEnabled)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(config.GetSection("AzureAd"));

    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();
}
else
{
    builder.Services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(
            DevelopmentAuthHandler.SchemeName, null);
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TesterOrAbove", policy => policy.RequireRole("Admin", "Tester"));
    options.AddPolicy("AnyAuthenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Ensure database is migrated and seeded
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TestRunnerDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database initialized");
    
    // Seed test data
    var seeder = new TestDataSeeder(dbContext);
    await seeder.SeedSampleDataAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapHealthChecks("/health").AllowAnonymous();

// Sign-in / sign-out controller routes (Microsoft.Identity.Web.UI)
if (authEnabled)
{
    app.MapControllers();
}

// REST API endpoints
var api = app.MapGroup("/api");

// Test Direct Line connection
api.MapPost("/test-connection", TestDirectLineConnection).RequireAuthorization("TesterOrAbove");

// Test Suites endpoints — read: any authenticated, write: tester+, delete: admin
api.MapGet("/testsuites", GetTestSuites).RequireAuthorization("AnyAuthenticated");
api.MapGet("/testsuites/{id}", GetTestSuite).RequireAuthorization("AnyAuthenticated");
api.MapPost("/testsuites", CreateTestSuite).RequireAuthorization("TesterOrAbove");
api.MapPut("/testsuites/{id}", UpdateTestSuite).RequireAuthorization("TesterOrAbove");
api.MapDelete("/testsuites/{id}", DeleteTestSuite).RequireAuthorization("AdminOnly");

// Runs endpoints
api.MapGet("/runs", GetRuns).RequireAuthorization("AnyAuthenticated");
api.MapGet("/runs/{id}", GetRun).RequireAuthorization("AnyAuthenticated");
api.MapPost("/runs", StartRun).RequireAuthorization("TesterOrAbove");
api.MapGet("/runs/{id}/results", GetRunResults).RequireAuthorization("AnyAuthenticated");

// Results endpoint
api.MapGet("/results/{id}/transcript", GetResultTranscript).RequireAuthorization("AnyAuthenticated");

// Documents endpoints
api.MapGet("/documents", GetDocuments).RequireAuthorization("AnyAuthenticated");
api.MapPost("/documents", UploadDocument).RequireAuthorization("TesterOrAbove");
api.MapDelete("/documents/{id}", DeleteDocument).RequireAuthorization("AdminOnly");

// Metrics/Dashboard
api.MapGet("/metrics/summary", GetMetricsSummary).RequireAuthorization("AnyAuthenticated");

async Task<IResult> GetTestSuites(TestRunnerDbContext db)
{
    var suites = await db.TestSuites.ToListAsync();
    return Results.Ok(suites);
}

async Task<IResult> GetTestSuite(Guid id, TestRunnerDbContext db)
{
    var suite = await db.TestSuites.Include(s => s.TestCases).FirstOrDefaultAsync(s => s.Id == id);
    return suite == null ? Results.NotFound() : Results.Ok(suite);
}

async Task<IResult> CreateTestSuite(CreateTestSuiteRequest request, TestRunnerDbContext db, ClaimsPrincipal user)
{
    var suite = new TestSuite
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description ?? "",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = user.Identity?.Name ?? request.CreatedBy ?? "unknown"
    };
    db.TestSuites.Add(suite);
    await db.SaveChangesAsync();
    return Results.Created($"/api/testsuites/{suite.Id}", suite);
}

async Task<IResult> UpdateTestSuite(Guid id, UpdateTestSuiteRequest request, TestRunnerDbContext db)
{
    var suite = await db.TestSuites.FindAsync(id);
    if (suite == null) return Results.NotFound();
    
    suite.Name = request.Name ?? suite.Name;
    suite.Description = request.Description ?? suite.Description;
    suite.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(suite);
}

async Task<IResult> DeleteTestSuite(Guid id, TestRunnerDbContext db)
{
    var suite = await db.TestSuites.FindAsync(id);
    if (suite == null) return Results.NotFound();
    
    db.TestSuites.Remove(suite);
    await db.SaveChangesAsync();
    return Results.NoContent();
}

async Task<IResult> GetRuns(TestRunnerDbContext db)
{
    var runs = await db.Runs.Include(r => r.Suite).OrderByDescending(r => r.StartedAt).ToListAsync();
    return Results.Ok(runs);
}

async Task<IResult> GetRun(Guid id, TestRunnerDbContext db)
{
    var run = await db.Runs.Include(r => r.Suite).Include(r => r.Results).FirstOrDefaultAsync(r => r.Id == id);
    return run == null ? Results.NotFound() : Results.Ok(run);
}

async Task<IResult> StartRun(StartRunRequest request, TestRunnerDbContext db, ITestExecutionService executionService, IJudgeService judgeService, IMultiAgentExecutionCoordinator coordinator, IAgentConfigurationService agentConfig, TestRunnerConfiguration config)
{
    var suite = await db.TestSuites
        .Include(s => s.TestSuiteAgents)
        .ThenInclude(tsa => tsa.Agent)
        .FirstOrDefaultAsync(s => s.Id == request.SuiteId);
    
    if (suite == null) 
        return Results.NotFound("Test suite not found");
    
    // Determine which agents to run against
    List<Agent> agentsToRun = new();
    
    if (request.AgentIds != null && request.AgentIds.Any())
    {
        // Use specified agents
        agentsToRun = await db.Agents
            .Where(a => request.AgentIds.Contains(a.Id))
            .ToListAsync();
    }
    else if (suite.TestSuiteAgents.Any())
    {
        // Use agents linked to suite
        agentsToRun = suite.TestSuiteAgents.Select(tsa => tsa.Agent).ToList();
    }
    else
    {
        // Fall back to creating a run with global config (legacy behavior)
        var judgeSettings = new JudgeSetting
        {
            Temperature = config.Judge.Temperature,
            TopP = config.Judge.TopP,
            MaxOutputTokens = config.Judge.MaxOutputTokens,
            PassThreshold = 0.7,
            Endpoint = config.Judge.Endpoint,
            ApiKey = config.Judge.ApiKey,
            Model = config.Judge.Model
        };
        
        var dlClient = new DirectLineClient(
            config.DirectLine.Secret,
            config.DirectLine.BotId,
            config.DirectLine.ReplyTimeoutSeconds,
            config.DirectLine.UseWebChannelSecret);
        
        var run = await executionService.ExecuteTestSuiteAsync(
            request.SuiteId, db, dlClient, judgeService, judgeSettings, config.DirectLine);
        return Results.Created($"/api/runs/{run.Id}", run);
    }
    
    // Execute against configured agents
    var runs = await coordinator.ExecuteForMultipleAgentsAsync(
        request.SuiteId,
        agentsToRun,
        db,
        judgeService,
        agentConfig,
        delayBetweenTestsMs: 2000);
    
    return Results.Created($"/api/runs", new { runs = runs, count = runs.Count });
}

async Task<IResult> GetRunResults(Guid id, TestRunnerDbContext db)
{
    var results = await db.Results.Where(r => r.RunId == id).ToListAsync();
    return Results.Ok(results);
}

async Task<IResult> GetResultTranscript(Guid id, TestRunnerDbContext db)
{
    var transcript = await db.TranscriptMessages.Where(m => m.ResultId == id).OrderBy(m => m.SequenceNumber).ToListAsync();
    return Results.Ok(transcript);
}

async Task<IResult> GetDocuments(TestRunnerDbContext db)
{
    var docs = await db.Documents.ToListAsync();
    return Results.Ok(docs);
}

async Task<IResult> UploadDocument(IFormFile file, TestRunnerDbContext db, IDocumentIngestor ingestor, IDocumentChunker chunker)
{
    if (file.Length == 0) return Results.BadRequest("No file provided");
    
    try
    {
        using var stream = file.OpenReadStream();
        var (text, pageCount) = await ingestor.ExtractTextAsync(stream, System.IO.Path.GetExtension(file.FileName).TrimStart('.'));
        
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = file.FileName,
            DocumentType = System.IO.Path.GetExtension(file.FileName).TrimStart('.').ToLower(),
            FileSizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            ContentHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))
        };
        
        // Create chunks
        var chunks = chunker.ChunkText(text);
        foreach (var chunk in chunks)
        {
            document.Chunks.Add(new Chunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                Text = chunk.Text,
                TokenCount = chunk.TokenEstimate,
                ChunkIndex = chunk.Index
            });
        }
        
        db.Documents.Add(document);
        await db.SaveChangesAsync();
        
        return Results.Created($"/api/documents/{document.Id}", document);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Upload error: {ex.Message}");
    }
}

async Task<IResult> DeleteDocument(Guid id, TestRunnerDbContext db)
{
    var doc = await db.Documents.FindAsync(id);
    if (doc == null) return Results.NotFound();
    
    db.Documents.Remove(doc);
    await db.SaveChangesAsync();
    return Results.NoContent();
}

async Task<IResult> GetMetricsSummary(TestRunnerDbContext db)
{
    var totalRuns = await db.Runs.CountAsync();
    var totalTests = await db.TestCases.CountAsync();
    var results = await db.Results.ToListAsync();
    var passCount = results.Count(r => r.Verdict == "pass");
    var failCount = results.Count(r => r.Verdict == "fail");
    
    var summary = new
    {
        totalRuns,
        totalTests,
        totalTestCases = results.Count,
        passCount,
        failCount,
        passRate = results.Count > 0 ? (100.0 * passCount / results.Count) : 0,
        avgLatency = results.Count > 0 ? results.Average(r => r.LatencyMs) : 0,
        medianLatency = results.Count > 0 ? CalculateMedian(results.Select(r => r.LatencyMs).ToList()) : 0
    };
    
    return Results.Ok(summary);
}

static double CalculateMedian(List<long> values)
{
    if (values.Count == 0) return 0;
    var sorted = values.OrderBy(v => v).ToList();
    return (sorted[values.Count / 2] + sorted[(values.Count - 1) / 2]) / 2.0;
}

async Task<IResult> TestDirectLineConnection(TestRunnerConfiguration config)
{
    try
    {
        var dlConfig = config.DirectLine;
        var client = new DirectLineClient(dlConfig.Secret, dlConfig.BotId, dlConfig.ReplyTimeoutSeconds, dlConfig.UseWebChannelSecret);
        
        Log.Information("API: TestDirectLineConnection starting");
        
        // Start conversation
        var conversationId = await client.StartConversationAsync();
        if (string.IsNullOrEmpty(conversationId))
            return Results.BadRequest("Failed to get conversation ID");
        
        Log.Information("API: Conversation ID: {ConversationId}", conversationId);
        
        // Send message
        await client.SendActivityAsync(conversationId, "Hello");
        Log.Information("API: Message sent");
        
        // Poll for response
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(dlConfig.ReplyTimeoutSeconds);
        var activities = new List<Activity>();
        var watermark = string.Empty;
        
        while (stopwatch.Elapsed < timeout)
        {
            var response = await client.GetActivitiesAsync(conversationId, watermark);
            activities = response.activities;
            if (!string.IsNullOrEmpty(response.watermark))
            {
                watermark = response.watermark;
            }
            var botMessages = activities.Where(a => a.Type == "message" && a.From?.Id != "user").ToList();
            
            if (botMessages.Count > 0)
            {
                Log.Information("API: Response found! {Count} bot messages", botMessages.Count);
                return Results.Ok(new { success = true, message = botMessages.First().Text });
            }
            
            await Task.Delay(500);
        }
        
        Log.Information("API: No response after {TimeoutSeconds} seconds", dlConfig.ReplyTimeoutSeconds);
        return Results.BadRequest($"Agent did not respond within {dlConfig.ReplyTimeoutSeconds} seconds");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "API: TestDirectLineConnection failed");
        return Results.BadRequest($"Connection test failed: {ex.Message}");
    }
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Log.Information("Starting CopilotStudioTestRunner WebUI");
app.Run();
