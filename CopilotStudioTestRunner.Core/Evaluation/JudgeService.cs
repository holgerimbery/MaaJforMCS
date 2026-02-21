using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using CopilotStudioTestRunner.Domain.Configuration;
using CopilotStudioTestRunner.Domain.Entities;
using Serilog;

namespace CopilotStudioTestRunner.Core.Evaluation;

public class JudgeEvaluationResult
{
    public double? TaskSuccess { get; set; }
    public double? IntentMatch { get; set; }
    public double? Factuality { get; set; }
    public double? Helpfulness { get; set; }
    public double? Safety { get; set; }
    public string Verdict { get; set; } = "unknown"; // pass, fail
    public string? Rationale { get; set; }
    public List<string> Citations { get; set; } = [];
}

/// <summary>
/// LLM-based judge for evaluating test case responses
/// </summary>
public interface IJudgeService
{
    Task<JudgeEvaluationResult> EvaluateAsync(
        JudgeSetting judgeSettings,
        TestCase testCase,
        List<TranscriptMessage> transcript,
        CancellationToken cancellationToken = default);
}

public class AzureAIFoundryJudgeService : IJudgeService
{
    private readonly ILogger _logger = Log.ForContext<AzureAIFoundryJudgeService>();

    public async Task<JudgeEvaluationResult> EvaluateAsync(
        JudgeSetting judgeSettings,
        TestCase testCase,
        List<TranscriptMessage> transcript,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build the judge prompt
            var systemPrompt = BuildSystemPrompt(judgeSettings);
            var userPrompt = BuildUserPrompt(testCase, transcript, judgeSettings);

            _logger.Debug("Evaluating test case: {TestCaseName}", testCase.Name);

            // Call Azure OpenAI API
            if (string.IsNullOrEmpty(judgeSettings.Endpoint) || string.IsNullOrEmpty(judgeSettings.ApiKey))
                throw new InvalidOperationException(
                    "Judge LLM endpoint and API key are required. Configure them on the agent or override them in the rubric.");

            var client = new AzureOpenAIClient(
                new Uri(judgeSettings.Endpoint),
                new AzureKeyCredential(judgeSettings.ApiKey));

            var chatClient = client.GetChatClient(judgeSettings.Model ?? "gpt-4o-mini");
            
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            _logger.Information("Calling Azure OpenAI for evaluation. Endpoint: {Endpoint}, Model: {Model}",
                judgeSettings.Endpoint, judgeSettings.Model);

            var chatCompletion = await chatClient.CompleteChatAsync(messages, options: null, cancellationToken);

            var responseContent = chatCompletion.Value.Content[0].Text;
            _logger.Debug("Azure OpenAI response: {Response}", responseContent);

            var result = ParseJudgeResponse(responseContent, judgeSettings);
            
            _logger.Information("Evaluation completed for {TestCaseName}: Verdict={Verdict}, Score={TaskSuccess}",
                testCase.Name, result.Verdict, result.TaskSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Judge evaluation failed for test case: {TestCaseName}", testCase.Name);
            return new JudgeEvaluationResult
            {
                Verdict = "error",
                Rationale = $"Evaluation error: {ex.Message}"
            };
        }
    }

    private string BuildSystemPrompt(JudgeSetting judgeSettings)
    {
        return judgeSettings.PromptTemplate?.Length > 0
            ? judgeSettings.PromptTemplate
            : """
            You are an impartial evaluator of conversational AI responses. Your task is to score the agent's response to the user's request.
            
            CRITICAL EVALUATION RULES:
            
            1. CITATIONS ARE POSITIVE: Responses with citations (e.g., "[1]: cite:...") indicate the bot sourced information from knowledge bases. This is GOOD and shows grounding.
            
            2. SEMANTIC EQUIVALENCE: Check if response contains key information from the reference answer, even if phrased differently.
               Examples of equivalent answers:
               - "processed within 10 business days" ≈ "refund processing takes 10 business days"
               - "return within 7 days" ≈ "7-day return window"
               - "original packaging required" ≈ "must be in original packaging"
               
            3. CONTENT vs CITATIONS: Separate the actual content from citation blocks when evaluating. 
               - Extract the main response text (ignore citation references at the end)
               - Compare the extracted content against the reference answer
               - Example: "Product must be in original packaging[1]" → evaluate as "Product must be in original packaging"
            
            4. PASS CRITERIA: Response contains KEY INFORMATION from reference answer AND falls into one of these categories:
               a) Exact or semantically equivalent wording (with or without citations)
               b) Paraphrased but factually correct content (with or without citations)
               c) Content with citations showing grounding in sources
            
            5. FAIL CRITERIA: Response ONLY fails if:
               - Critical information is missing (not paraphrased, just absent)
               - Information contradicts the reference answer
               - Response is completely irrelevant or off-topic
            
            Evaluate based on these dimensions:
            - Task Success: Did the agent successfully address the user's request AND provide key information from reference answer? (0-1 score)
            - Intent Match: Does the response match the expected intent and provide relevant information? (0-1 score)
            - Factuality: Is the information accurate, consistent with reference, and grounded (citations count as grounding)? (0-1 score)
            - Helpfulness: Is the response complete, clear, with essential information? (0-1 score)
            - Safety: Does the response follow safety guidelines? (0-1 score)
            
            Respond with a JSON object containing these exact fields:
            {
              "task_success": <0-1>,
              "intent_match": <0-1>,
              "factuality": <0-1>,
              "helpfulness": <0-1>,
              "safety": <0-1>,
              "verdict": "pass|fail",
              "rationale": "explanation",
              "citations": ["reference1", "reference2"]
            }
            """;
    }

    private string BuildUserPrompt(TestCase testCase, List<TranscriptMessage> transcript, JudgeSetting judgeSettings)
    {
        var transcriptText = string.Join("\n", transcript.Select(m =>
            $"[{m.Timestamp:HH:mm:ss}] {m.Role.ToUpper()}: {m.Content}"));

        var refAnswer = string.IsNullOrEmpty(testCase.ReferenceAnswer) ? "None" : testCase.ReferenceAnswer;
        var expectedEntities = testCase.ExpectedEntities.Length > 0
            ? string.Join(", ", testCase.ExpectedEntities)
            : "None";

        return $"""
        Test Case: {testCase.Name}
        Description: {testCase.Description}
        Expected Intent: {testCase.ExpectedIntent ?? "Not specified"}
        Expected Entities: {expectedEntities}
        Acceptance Criteria: {testCase.AcceptanceCriteria}
        Reference Answer: {refAnswer}
        
        Conversation Transcript:
        {transcriptText}
        
        EVALUATION INSTRUCTIONS:
        1. Extract the main response content from the agent (ignore citation blocks like "[1]: cite:..." at the end).
        2. Compare extracted content against the reference answer for KEY INFORMATION match.
        3. Accept paraphrasing, semantic equivalence, and different phrasings of the same concept.
        4. Citations are POSITIVE indicators - they show the bot grounded its answer in sources. Score high if core info is present + cited.
        5. Score high on task_success if: core information is present (paraphrased or exact) AND citations show grounding.
        6. Only mark as FAIL if: critical information is completely missing OR information contradicts the reference.
        7. Accept different formatting (bullet points, paragraphs, lists) as long as information is equivalent.
        
        Example:
        - Reference: "Smart Brew 300 can be returned within 7 days and must be in original packaging."
        - Given: "The Smart Brew 300 has a 7-day return window and requires original packaging[1]."
        - Verdict: PASS (same info, different wording, citations show grounding)
        
        Please evaluate the agent's response based on the criteria above.
        """;
    }

    private JudgeEvaluationResult ParseJudgeResponse(string response, JudgeSetting judgeSettings)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                _logger.Warning("Could not find JSON in judge response");
                return new JudgeEvaluationResult { Verdict = "error" };
            }

            var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart);
            var result = System.Text.Json.JsonSerializer.Deserialize<JudgeResponseJson>(jsonStr);

            if (result == null)
            {
                return new JudgeEvaluationResult { Verdict = "error" };
            }

            var evaluation = new JudgeEvaluationResult
            {
                TaskSuccess = result.task_success,
                IntentMatch = result.intent_match,
                Factuality = result.factuality,
                Helpfulness = result.helpfulness,
                Safety = result.safety,
                Rationale = result.rationale,
                Citations = result.citations ?? []
            };

            // Calculate weighted score
            var weights = new[]
            {
                (result.task_success ?? 0) * judgeSettings.TaskSuccessWeight,
                (result.intent_match ?? 0) * judgeSettings.IntentMatchWeight,
                (result.factuality ?? 0) * judgeSettings.FactualityWeight,
                (result.helpfulness ?? 0) * judgeSettings.HelpfulnessWeight,
                (result.safety ?? 0) * judgeSettings.SafetyWeight
            };

            var overallScore = weights.Sum();
            evaluation.Verdict = overallScore >= judgeSettings.PassThreshold ? "pass" : "fail";

            _logger.Debug("Judge evaluation: {Verdict} (score: {Score:P})", evaluation.Verdict, overallScore);

            return evaluation;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to parse judge response");
            return new JudgeEvaluationResult { Verdict = "error", Rationale = $"Parse error: {ex.Message}" };
        }
    }

    private string ExtractModelName(string endpoint)
    {
        // Extract model name from Azure AI Foundry endpoint
        // Format: https://<resource>.openai.azure.com/openai/deployments/<deployment-name>/
        var parts = endpoint.Split('/');
        var deployments = Array.IndexOf(parts, "deployments");
        return deployments >= 0 && deployments + 1 < parts.Length ? parts[deployments + 1] : "gpt-4o-mini";
    }
}

internal class JudgeResponseJson
{
    [JsonPropertyName("task_success")]
    public double? task_success { get; set; }

    [JsonPropertyName("intent_match")]
    public double? intent_match { get; set; }

    [JsonPropertyName("factuality")]
    public double? factuality { get; set; }

    [JsonPropertyName("helpfulness")]
    public double? helpfulness { get; set; }

    [JsonPropertyName("safety")]
    public double? safety { get; set; }

    [JsonPropertyName("verdict")]
    public string? verdict { get; set; }

    [JsonPropertyName("rationale")]
    public string? rationale { get; set; }

    [JsonPropertyName("citations")]
    public List<string>? citations { get; set; }
}
