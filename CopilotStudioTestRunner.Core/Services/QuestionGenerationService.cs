using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using CopilotStudioTestRunner.Domain.Entities;
using Serilog;

namespace CopilotStudioTestRunner.Core.Services;

public class GeneratedQuestion
{
    public string Question { get; set; } = string.Empty;
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string ExpectedIntent { get; set; } = string.Empty;
    public List<string> ExpectedEntities { get; set; } = new();
    public string Context { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
}

public class QuestionGenerationRequest
{
    public string DocumentContent { get; set; } = string.Empty;
    public int NumberOfQuestions { get; set; } = 5;
    public string? Domain { get; set; }
    public List<string>? ExistingQuestions { get; set; }
}

/// <summary>
/// Azure OpenAI-powered service for generating contextual test questions from documents
/// </summary>
public interface IQuestionGenerationService
{
    Task<List<GeneratedQuestion>> GenerateQuestionsAsync(
        QuestionGenerationRequest request,
        string endpoint,
        string apiKey,
        string model,
        CancellationToken cancellationToken = default);
}

public class AzureOpenAIQuestionGenerationService : IQuestionGenerationService
{
    private readonly ILogger _logger = Log.ForContext<AzureOpenAIQuestionGenerationService>();

    public async Task<List<GeneratedQuestion>> GenerateQuestionsAsync(
        QuestionGenerationRequest request,
        string endpoint,
        string apiKey,
        string model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = BuildSystemPrompt(request);
            var userPrompt = BuildUserPrompt(request);

            _logger.Information("Generating {Count} questions from document content (length: {Length})",
                request.NumberOfQuestions, request.DocumentContent.Length);

            // Call Azure OpenAI API
            var client = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));

            var chatClient = client.GetChatClient(model);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatCompletion = await chatClient.CompleteChatAsync(messages, options: null, cancellationToken);
            var responseContent = chatCompletion.Value.Content[0].Text;

            _logger.Debug("Azure OpenAI response: {Response}", responseContent);

            var questions = ParseQuestionResponse(responseContent);

            _logger.Information("Successfully generated {Count} questions", questions.Count);

            return questions;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Question generation failed");
            return new List<GeneratedQuestion>();
        }
    }

    private string BuildSystemPrompt(QuestionGenerationRequest request)
    {
        var domainContext = !string.IsNullOrEmpty(request.Domain)
            ? $"The domain/topic is: {request.Domain}"
            : "Analyze the domain based on the document content.";

        return $$"""
        You are an expert test designer for conversational AI agents. Your task is to generate high-quality test questions 
        from document content that will thoroughly test an AI agent's understanding and capabilities.

        {{domainContext}}

        Generate questions that:
        1. **Cover diverse aspects**: Don't just focus on one topic - spread questions across different sections and concepts
        2. **Vary in complexity**: Mix factual recall, reasoning, and application questions
        3. **Test edge cases**: Include questions about specific details, ambiguous scenarios, and corner cases
        4. **Are realistic**: Frame questions as a real user would ask them
        5. **Have clear answers**: Each question should have a verifiable answer from the document

        Question types to include:
        - Direct factual questions (What/When/Where)
        - Conceptual understanding (How/Why)
        - Comparison questions (Difference between X and Y)
        - Application questions (How would I do X?)
        - Troubleshooting/problem-solving questions

        For each question, provide:
        - **question**: The user's question (natural, conversational)
        - **expected_answer**: The ideal answer based on the document
        - **expected_intent**: What the user is trying to accomplish (e.g., "get_product_info", "troubleshoot_issue", "compare_options")
        - **expected_entities**: Key entities/concepts in the question (e.g., ["product_name", "feature_name"])
        - **context**: The relevant snippet from the document
        - **rationale**: Why this question is important for testing

        Respond with a JSON array:
        [
          {
            "question": "...",
            "expected_answer": "...",
            "expected_intent": "...",
            "expected_entities": ["...", "..."],
            "context": "...",
            "rationale": "..."
          }
        ]
        """;
    }

    private string BuildUserPrompt(QuestionGenerationRequest request)
    {
        var existingQuestionsText = request.ExistingQuestions?.Count > 0
            ? $"\n\nAvoid generating questions similar to these existing ones:\n{string.Join("\n", request.ExistingQuestions.Select((q, i) => $"{i + 1}. {q}"))}"
            : "";

        return $$"""
        Generate {{request.NumberOfQuestions}} diverse test questions from the following document content.

        DOCUMENT CONTENT:
        {{request.DocumentContent}}
        {{existingQuestionsText}}

        Ensure questions are:
        - Spread across different topics/sections in the document
        - Varied in difficulty and question type
        - Natural and conversational
        - Testable with clear expected answers

        Return ONLY a valid JSON array with {{request.NumberOfQuestions}} question objects.
        """;
    }

    private List<GeneratedQuestion> ParseQuestionResponse(string response)
    {
        try
        {
            // Try to extract JSON array from the response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;

            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                _logger.Warning("Could not find JSON array in response");
                return new List<GeneratedQuestion>();
            }

            var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var questionDtos = JsonSerializer.Deserialize<List<QuestionDto>>(jsonStr, options);

            if (questionDtos == null || questionDtos.Count == 0)
            {
                _logger.Warning("No questions found in response");
                return new List<GeneratedQuestion>();
            }

            return questionDtos.Select(dto => new GeneratedQuestion
            {
                Question = dto.question ?? string.Empty,
                ExpectedAnswer = dto.expected_answer ?? string.Empty,
                ExpectedIntent = dto.expected_intent ?? "general_inquiry",
                ExpectedEntities = dto.expected_entities ?? new List<string>(),
                Context = dto.context ?? string.Empty,
                Rationale = dto.rationale ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to parse question generation response");
            return new List<GeneratedQuestion>();
        }
    }

    private class QuestionDto
    {
        [JsonPropertyName("question")]
        public string? question { get; set; }

        [JsonPropertyName("expected_answer")]
        public string? expected_answer { get; set; }

        [JsonPropertyName("expected_intent")]
        public string? expected_intent { get; set; }

        [JsonPropertyName("expected_entities")]
        public List<string>? expected_entities { get; set; }

        [JsonPropertyName("context")]
        public string? context { get; set; }

        [JsonPropertyName("rationale")]
        public string? rationale { get; set; }
    }
}
