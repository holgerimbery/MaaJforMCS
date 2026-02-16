// Test AI-powered question generation from documents
#r "nuget: Azure.AI.OpenAI, 2.2.0"
#r "nuget: Serilog, 4.2.0"
#r "nuget: Serilog.Sinks.Console, 6.0.0"
#r "bin/Debug/net9.0/CopilotStudioTestRunner.Core.dll"

using System;
using System.Threading.Tasks;
using CopilotStudioTestRunner.Core.Services;
using Serilog;

// Enable detailed logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// ============================================================================
// CONFIGURATION - Update these values with your Azure OpenAI credentials
// ============================================================================
var azureOpenAIEndpoint = "https://YOUR-RESOURCE.openai.azure.com";  // Your Azure OpenAI endpoint
var azureOpenAIKey = "YOUR-API-KEY";                                 // Your Azure OpenAI API key
var deploymentModel = "gpt-4o-mini";                                 // Your deployment name (gpt-4o, gpt-4o-mini, etc.)

// Sample document content (replace with actual document content)
var sampleDocument = @"
Microsoft Copilot Studio is a comprehensive platform for building intelligent conversational AI agents. 
It provides a low-code interface that enables both developers and business users to create sophisticated bots 
without extensive programming knowledge.

Key Features:
1. Visual Dialog Designer: Create conversation flows using an intuitive drag-and-drop interface
2. Natural Language Processing: Built-in AI understands user intent and extracts entities
3. Integration Hub: Connect to hundreds of data sources via Power Platform connectors
4. Multi-Channel Deployment: Deploy your bot to Microsoft Teams, websites, mobile apps, and more
5. Analytics Dashboard: Monitor bot performance, user satisfaction, and conversation success rates

Security and Compliance:
Copilot Studio agents can be configured with role-based access control, data loss prevention policies,
and comply with industry standards including GDPR, HIPAA, and SOC 2. All conversation data is encrypted
at rest and in transit.

Getting Started:
To create your first bot:
1. Sign in to Copilot Studio at https://copilotstudio.microsoft.com
2. Click 'Create' and select 'New agent'
3. Choose a template or start from scratch
4. Design your conversation flows using topics
5. Test your bot in the built-in test chat
6. Publish to your desired channels
";

// ============================================================================
// MAIN EXECUTION
// ============================================================================
Log.Information("=== AI-POWERED QUESTION GENERATION TEST ===");
Log.Information("Using Azure OpenAI: {Endpoint}", azureOpenAIEndpoint);
Log.Information("Model: {Model}", deploymentModel);

try
{
    var service = new AzureOpenAIQuestionGenerationService();

    // Test 1: Generate 5 diverse questions
    Log.Information("\n--- Test 1: Generate 5 diverse questions ---");
    var request1 = new QuestionGenerationRequest
    {
        DocumentContent = sampleDocument,
        NumberOfQuestions = 5,
        Domain = "Microsoft Copilot Studio"
    };

    var questions1 = await service.GenerateQuestionsAsync(
        request1,
        azureOpenAIEndpoint,
        azureOpenAIKey,
        deploymentModel
    );

    Log.Information("Generated {Count} questions:", questions1.Count);
    for (int i = 0; i < questions1.Count; i++)
    {
        var q = questions1[i];
        Log.Information("\n--- Question {Index} ---", i + 1);
        Log.Information("  Question: {Question}", q.Question);
        Log.Information("  Expected Answer: {Answer}", 
            q.ExpectedAnswer.Length > 100 ? q.ExpectedAnswer.Substring(0, 100) + "..." : q.ExpectedAnswer);
        Log.Information("  Intent: {Intent}", q.ExpectedIntent);
        Log.Information("  Entities: {Entities}", string.Join(", ", q.ExpectedEntities));
        Log.Information("  Rationale: {Rationale}", q.Rationale);
    }

    // Test 2: Generate additional questions avoiding duplicates
    Log.Information("\n\n--- Test 2: Generate 3 more questions (avoiding duplicates) ---");
    var existingQuestions = questions1.Select(q => q.Question).ToList();
    
    var request2 = new QuestionGenerationRequest
    {
        DocumentContent = sampleDocument,
        NumberOfQuestions = 3,
        Domain = "Microsoft Copilot Studio",
        ExistingQuestions = existingQuestions
    };

    var questions2 = await service.GenerateQuestionsAsync(
        request2,
        azureOpenAIEndpoint,
        azureOpenAIKey,
        deploymentModel
    );

    Log.Information("Generated {Count} additional questions:", questions2.Count);
    for (int i = 0; i < questions2.Count; i++)
    {
        var q = questions2[i];
        Log.Information("\n--- Question {Index} ---", i + 1);
        Log.Information("  Question: {Question}", q.Question);
        Log.Information("  Intent: {Intent}", q.ExpectedIntent);
        Log.Information("  Rationale: {Rationale}", q.Rationale);
    }

    // Summary
    Log.Information("\n\n=== SUMMARY ===");
    Log.Information("✓ Total questions generated: {Total}", questions1.Count + questions2.Count);
    Log.Information("✓ Question types covered: {Types}", 
        string.Join(", ", questions1.Concat(questions2).Select(q => q.ExpectedIntent).Distinct()));
    Log.Information("\n✓ AI-powered question generation is working!");
    Log.Information("\nNext steps:");
    Log.Information("1. Integrate this service into SetupWizard.razor");
    Log.Information("2. Configure Azure OpenAI settings in your application");
    Log.Information("3. Replace the current text-based GenerateQuestions() method");
}
catch (Exception ex)
{
    Log.Error(ex, "Question generation test failed");
    Log.Error("\nTroubleshooting:");
    Log.Error("1. Verify your Azure OpenAI endpoint and API key are correct");
    Log.Error("2. Ensure your deployment name matches an existing deployment");
    Log.Error("3. Check that your Azure OpenAI resource has sufficient quota");
    Log.Error("4. Verify network connectivity to Azure OpenAI");
}
