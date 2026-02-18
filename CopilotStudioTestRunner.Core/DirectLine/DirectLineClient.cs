using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace CopilotStudioTestRunner.Core.DirectLine;

/// <summary>
/// Direct Line activity models
/// </summary>
public class Activity
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";

    [JsonPropertyName("from")]
    public Attachment? From { get; set; }

    [JsonPropertyName("recipient")]
    public Attachment? Recipient { get; set; }

    [JsonPropertyName("conversation")]
    public Conversation? Conversation { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachment> Attachments { get; set; } = [];

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("replyToId")]
    public string? ReplyToId { get; set; }
}

public class Attachment
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("contentUrl")]
    public string? ContentUrl { get; set; }
}

public class Conversation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }
}

/// <summary>
/// Direct Line client for browser-based connections
/// </summary>
public interface IDirectLineClient
{
    Task<string> StartConversationAsync(CancellationToken cancellationToken = default);
    Task SendActivityAsync(string conversationId, string text, CancellationToken cancellationToken = default);
    Task<(List<Activity> activities, string watermark)> GetActivitiesAsync(string conversationId, string? watermark = null, CancellationToken cancellationToken = default);
    Task<(List<Activity> activities, string watermark)> StreamActivitiesWebSocketAsync(string conversationId, Func<Activity, Task> onActivityReceived, CancellationToken cancellationToken = default);
}

public class DirectLineClient : IDirectLineClient
{
    private readonly string _directLineSecret;
    private readonly string _botId;
    private readonly HttpClient _httpClient;
    private readonly int _replyTimeoutSeconds;
    private readonly bool _useWebChannelSecret;
    private readonly ILogger _logger = Log.ForContext<DirectLineClient>();
    private string? _conversationToken;

    private const string DirectLineEndpoint = "https://directline.botframework.com/v3/directline";

    public DirectLineClient(string directLineSecret, string botId, int replyTimeoutSeconds = 30, bool useWebChannelSecret = false)
    {
        _directLineSecret = directLineSecret;
        _botId = botId;
        _replyTimeoutSeconds = replyTimeoutSeconds;
        _useWebChannelSecret = useWebChannelSecret;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(replyTimeoutSeconds + 10)
        };
    }

    public async Task<string> StartConversationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // When using web channel secret, first get the authentication token
            if (_useWebChannelSecret)
            {
                var tokenResponse = await GenerateWebChannelTokenResponseAsync(cancellationToken);
                _conversationToken = tokenResponse.Token;
                _logger.Information("Generated web channel token, now creating REST API conversation");
                
                // Now use that token to create a Direct Line conversation via REST API
                var request = new HttpRequestMessage(HttpMethod.Post, $"{DirectLineEndpoint}/conversations");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _conversationToken);
                var response = await _httpClient.SendAsync(request, cancellationToken);

                var webChannelContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Failed to create conversation with web channel token. Status: {StatusCode}. Body: {Body}", 
                        (int)response.StatusCode, webChannelContent);
                    throw new InvalidOperationException($"Failed to create conversation: HTTP {(int)response.StatusCode}");
                }

                _logger.Information("Conversation creation response: {Body}", webChannelContent);
                var responseObj = JsonSerializer.Deserialize<WebChannelTokenResponse>(webChannelContent);
                
                _logger.Information("Deserialized conversation ID: {ConversationId}", responseObj?.ConversationId);
                return responseObj?.ConversationId ?? throw new InvalidOperationException("No conversation ID returned from Direct Line");
            }

            // Standard Direct Line token flow
            var token = await GenerateConversationTokenAsync(cancellationToken);
            var request2 = new HttpRequestMessage(HttpMethod.Post, $"{DirectLineEndpoint}/conversations");
            request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response2 = await _httpClient.SendAsync(request2, cancellationToken);

            if (!response2.IsSuccessStatusCode)
            {
                var responseBody = await response2.Content.ReadAsStringAsync(cancellationToken);
                _logger.Error("Failed to start conversation. Status: {StatusCode}. Body: {Body}", 
                    (int)response2.StatusCode, responseBody);
                
                var errorMsg = response2.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Invalid or expired Direct Line secret",
                    System.Net.HttpStatusCode.Forbidden => "Access denied. Check if your Bot ID and secret are correct and valid",
                    _ => $"HTTP {(int)response2.StatusCode}: {responseBody}"
                };
                throw new InvalidOperationException(errorMsg);
            }

            var content = await response2.Content.ReadAsStringAsync(cancellationToken);
            var conversation = JsonSerializer.Deserialize<Conversation>(content);

            var conversationId = conversation?.Id ?? conversation?.ConversationId;
            
            if (string.IsNullOrEmpty(conversationId))
            {
                _logger.Warning("Start conversation response missing id. Status: {StatusCode}. Body: {Body}",
                    (int)response2.StatusCode,
                    content);
                throw new InvalidOperationException("Failed to start conversation: no ID returned");
            }

            _logger.Information("Started Direct Line conversation: {ConversationId}", conversationId);
            return conversationId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start Direct Line conversation");
            throw;
        }
    }

    public async Task SendActivityAsync(string conversationId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var activity = new Activity
            {
                Type = "message",
                Text = text,
                From = new Attachment { Id = "user" }
            };

            var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var token = await GetAuthTokenAsync(cancellationToken);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{DirectLineEndpoint}/conversations/{conversationId}/activities")
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            _logger.Debug("Sending activity to {Url} with token {TokenLength} chars", 
                request.RequestUri, token?.Length ?? 0);
            
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.Error("Failed to send activity. Status: {StatusCode}. Body: {Body}", 
                    (int)response.StatusCode, responseBody);
                throw new InvalidOperationException($"Failed to send message: HTTP {(int)response.StatusCode}");
            }

            _logger.Debug("Sent message to Direct Line conversation: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send activity to Direct Line");
            throw;
        }
    }

    public async Task<(List<Activity> activities, string watermark)> GetActivitiesAsync(string conversationId, string? watermark = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{DirectLineEndpoint}/conversations/{conversationId}/activities";
            if (!string.IsNullOrEmpty(watermark))
            {
                url += $"?watermark={watermark}";
            }

            var token = await GetAuthTokenAsync(cancellationToken);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            _logger.Debug("Getting activities from {Url} with token {TokenLength} chars", 
                url, token?.Length ?? 0);
            
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.Error("Failed to get activities. Status: {StatusCode}. Body: {Body}", 
                    (int)response.StatusCode, responseBody);
                throw new InvalidOperationException($"Failed to retrieve messages: HTTP {(int)response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var list = JsonSerializer.Deserialize<ActivityList>(content);
            var activities = list?.Activities ?? [];
            var nextWatermark = list?.Watermark ?? watermark ?? string.Empty;

            _logger.Debug("Retrieved {Count} activities from Direct Line", activities.Count);
            return (activities, nextWatermark);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get activities from Direct Line");
            throw;
        }
    }

    public async Task<(List<Activity> activities, string watermark)> StreamActivitiesWebSocketAsync(
        string conversationId,
        Func<Activity, Task> onActivityReceived,
        CancellationToken cancellationToken = default)
    {
        var activities = new List<Activity>();
        string watermark = "";

        try
        {
            // WebSocket support is limited in Direct Line, so we'll use polling as fallback
            _logger.Information("Direct Line WebSocket streaming not implemented, using polling fallback");

            var pollInterval = TimeSpan.FromSeconds(1);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(_replyTimeoutSeconds);

            while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
            {
                var (responseActivities, responseWatermark) = await GetActivitiesAsync(conversationId, watermark, cancellationToken);

                foreach (var activity in responseActivities)
                {
                    if (activity.From?.Id != "user")
                    {
                        activities.Add(activity);
                        await onActivityReceived(activity);
                    }
                }

                if (!string.IsNullOrEmpty(responseWatermark))
                {
                    watermark = responseWatermark;
                }

                if (responseActivities.Count == 0)
                {
                    await Task.Delay(pollInterval, cancellationToken);
                }
            }

            return (activities, watermark);
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Activity streaming cancelled");
            return (activities, watermark);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stream activities");
            throw;
        }
    }

    private async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        if (!_useWebChannelSecret)
        {
            return _directLineSecret;
        }

        if (!string.IsNullOrWhiteSpace(_conversationToken))
        {
            return _conversationToken;
        }

        return await GenerateConversationTokenAsync(cancellationToken);
    }

    private async Task<string> GenerateConversationTokenAsync(CancellationToken cancellationToken)
    {
        if (!_useWebChannelSecret)
        {
            return _directLineSecret;
        }

        if (!string.IsNullOrWhiteSpace(_conversationToken))
        {
            return _conversationToken;
        }

        var tokenResponse = await GenerateWebChannelTokenResponseAsync(cancellationToken);
        _conversationToken = tokenResponse.Token;
        return _conversationToken;
    }

    private async Task<WebChannelTokenResponse> GenerateWebChannelTokenResponseAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{DirectLineEndpoint}/tokens/generate");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _directLineSecret);

        _logger.Information("Calling token endpoint: {Endpoint}", $"{DirectLineEndpoint}/tokens/generate");
        _logger.Debug("Secret length: {SecretLength} chars", _directLineSecret?.Length ?? 0);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("Token generation failed. Status: {StatusCode}. Body: {Body}", 
                (int)response.StatusCode, content);
            
            var errorMsg = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Invalid or expired web channel security secret. Verify the secret from Copilot Studio → Settings → Security → Web channel security. The secret may have been regenerated.",
                System.Net.HttpStatusCode.Forbidden => "Access denied to token endpoint (403 Forbidden). This usually means: 1) The secret is invalid or expired, 2) The agent may not have web channel security enabled, or 3) The secret was regenerated. Check Copilot Studio → Settings → Security → Web channel security and verify the secret hasn't changed.",
                _ => $"Token generation failed: HTTP {(int)response.StatusCode}"
            };
            throw new InvalidOperationException(errorMsg);
        }

        _logger.Debug("Token endpoint response: {Body}", content);
        
        var tokenResponse = JsonSerializer.Deserialize<WebChannelTokenResponse>(content);
        if (string.IsNullOrWhiteSpace(tokenResponse?.Token) || string.IsNullOrWhiteSpace(tokenResponse?.ConversationId))
        {
            _logger.Error("Token generation response missing required fields. Body: {Body}", content);
            throw new InvalidOperationException("Failed to generate token: missing token or conversationId in response");
        }

        _logger.Information("Generated web channel token for conversation: {ConversationId}. Token length: {TokenLength}", 
            tokenResponse.ConversationId, tokenResponse.Token?.Length ?? 0);
        return tokenResponse;
    }
}

internal class ActivityList
{
    [JsonPropertyName("activities")]
    public List<Activity> Activities { get; set; } = [];

    [JsonPropertyName("watermark")]
    public string? Watermark { get; set; }
}

internal class TokenResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

internal class WebChannelTokenResponse
{
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("streamUrl")]
    public string? StreamUrl { get; set; }
}
