using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text.Json;
using Serilog;

namespace CopilotStudioTestRunner.WebUI.Services;

public record DiscoveredEnvironment(
    string Id,
    string DisplayName,
    string EnvironmentType,
    string? OrgUrl,
    string State,
    string Region);

public record DiscoveredAgent(
    string BotId,
    string Name,
    string SchemaName,
    bool IsActive,
    string? Language,
    string? Description);

public interface IPowerPlatformDiscoveryService
{
    /// <summary>
    /// Lists all Power Platform environments accessible to the user.
    /// Requires an access token scoped to https://service.powerapps.com/.
    /// </summary>
    Task<List<DiscoveredEnvironment>> GetEnvironmentsAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Lists all Copilot Studio agents (bots) in a specific Dataverse environment.
    /// Requires an access token scoped to the organization URL.
    /// </summary>
    Task<List<DiscoveredAgent>> GetAgentsAsync(string orgUrl, string accessToken, CancellationToken ct = default);

    /// <summary>Acquires the token automatically from the supplied credential (e.g. AzureCliCredential).</summary>
    Task<List<DiscoveredEnvironment>> GetEnvironmentsAsync(TokenCredential credential, CancellationToken ct = default);

    /// <summary>Acquires the Dataverse token automatically from the supplied credential.</summary>
    Task<List<DiscoveredAgent>> GetAgentsAsync(string orgUrl, TokenCredential credential, CancellationToken ct = default);
}

public class PowerPlatformDiscoveryService : IPowerPlatformDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly Serilog.ILogger Logger = Log.ForContext<PowerPlatformDiscoveryService>();

    private const string BapApiUrl =
        "https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/environments?$expand=properties&api-version=2016-11-01";

    public PowerPlatformDiscoveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<DiscoveredEnvironment>> GetEnvironmentsAsync(string accessToken, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(BapApiUrl, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);

        var environments = new List<DiscoveredEnvironment>();

        if (!doc.RootElement.TryGetProperty("value", out var values))
            return environments;

        foreach (var env in values.EnumerateArray())
        {
            var id = env.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

            env.TryGetProperty("properties", out var props);
            var hasProps = props.ValueKind == JsonValueKind.Object;

            var displayName = hasProps && props.TryGetProperty("displayName", out var dn)
                ? dn.GetString() ?? id : id;

            var envSku = hasProps && props.TryGetProperty("environmentSku", out var sku)
                ? sku.GetString() ?? "Unknown" : "Unknown";

            var state = hasProps
                && props.TryGetProperty("states", out var states)
                && states.TryGetProperty("management", out var mgmt)
                && mgmt.TryGetProperty("id", out var stateId)
                ? stateId.GetString() ?? "Ready" : "Ready";

            string? orgUrl = null;
            if (hasProps && props.TryGetProperty("linkedEnvironmentMetadata", out var lem)
                && lem.ValueKind == JsonValueKind.Object)
            {
                // BAP API uses "instanceUrl" or "instanceURL" depending on the tenant/version
                orgUrl = TryGetStringCaseInsensitive(lem, "instanceUrl")
                      ?? TryGetStringCaseInsensitive(lem, "instanceURL")
                      ?? TryGetStringCaseInsensitive(lem, "instanceurl");
                orgUrl = orgUrl?.TrimEnd('/');

                if (orgUrl == null)
                    Logger.Warning("linkedEnvironmentMetadata found but no instanceUrl for env {Id}. Keys: {Keys}",
                        id, string.Join(", ", lem.EnumerateObject().Select(p => p.Name)));
            }

            var region = env.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : "";

            environments.Add(new DiscoveredEnvironment(id, displayName, envSku, orgUrl, state, region));
        }

        Logger.Information("Discovered {Count} Power Platform environments", environments.Count);
        return environments;
    }

    public async Task<List<DiscoveredAgent>> GetAgentsAsync(string orgUrl, string accessToken, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var agents = new List<DiscoveredAgent>();
        // Use only the four core fields that exist on every Dataverse bot table version.
        // description/language are omitted — they cause 400 on some tenant configurations.
        // $orderby is intentionally omitted — it also causes 400 on some environments.
        var url = $"{orgUrl.TrimEnd('/')}/api/data/v9.2/bots?$select=name,botid,schemaname,statuscode";

        while (!string.IsNullOrEmpty(url))
        {
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                Logger.Warning("Failed to list bots at {OrgUrl}: HTTP {Status} Body: {Body}", orgUrl, (int)response.StatusCode, body);
                // Extract OData error message if present
                string detail = body;
                try
                {
                    var errDoc = JsonDocument.Parse(body);
                    if (errDoc.RootElement.TryGetProperty("error", out var errEl)
                        && errEl.TryGetProperty("message", out var msg))
                        detail = msg.GetString() ?? body;
                }
                catch { /* ignore parse errors */ }
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode}: {detail}",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("value", out var values))
            {
                foreach (var bot in values.EnumerateArray())
                {
                    var botId = bot.TryGetProperty("botid", out var bid) ? bid.GetString() ?? "" : "";
                    var name = bot.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
                    var schema = bot.TryGetProperty("schemaname", out var sc) ? sc.GetString() ?? "" : "";
                    var statusCode = bot.TryGetProperty("statuscode", out var sc2) ? sc2.GetInt32() : 0;
                    var desc = bot.TryGetProperty("description", out var d) && d.ValueKind != JsonValueKind.Null
                        ? d.GetString() : null;
                    // language is omitted from $select — read it only if present
                    var lang = bot.TryGetProperty("language", out var l) && l.ValueKind == JsonValueKind.String
                        ? l.GetString() : null;

                    agents.Add(new DiscoveredAgent(botId, name, schema, statusCode == 1, lang, desc));
                }
            }

            url = doc.RootElement.TryGetProperty("@odata.nextLink", out var next)
                  && next.ValueKind != JsonValueKind.Null
                ? next.GetString() ?? "" : "";
        }

        Logger.Information("Discovered {Count} agents in {OrgUrl}", agents.Count, orgUrl);
        return agents;
    }

    public async Task<List<DiscoveredEnvironment>> GetEnvironmentsAsync(TokenCredential credential, CancellationToken ct = default)
    {
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://api.powerapps.com/.default" }), ct);
        return await GetEnvironmentsAsync(token.Token, ct);
    }

    public async Task<List<DiscoveredAgent>> GetAgentsAsync(string orgUrl, TokenCredential credential, CancellationToken ct = default)
    {
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { $"{orgUrl.TrimEnd('/')}/.default" }), ct);
        return await GetAgentsAsync(orgUrl, token.Token, ct);
    }

    /// <summary>
    /// Returns a credential chain that works across all hosting environments:
    ///   1. EnvironmentCredential     — AZURE_CLIENT_ID/SECRET/TENANT env vars (Docker / CI)
    ///   2. AzureCliCredential        — developer machine (az login)
    ///   3. AzurePowerShellCredential — developer machine fallback (Connect-AzAccount)
    ///   4. ManagedIdentityCredential — Azure-hosted containers/VMs with a managed identity
    ///
    /// ManagedIdentityCredential is intentionally last: on a local machine it times out
    /// against the IMDS endpoint (169.254.169.254) and throws AuthenticationFailedException
    /// which stops the chain. Developer credentials are tried first so local use is fast.
    /// </summary>
    public static TokenCredential CreateDefaultCredential()
    {
        EnsureAzCliInPath();
        return new ChainedTokenCredential(
            new EnvironmentCredential(),
            new AzureCliCredential(),
            new AzurePowerShellCredential(),
            new ManagedIdentityCredential());
    }

    private static void EnsureAzCliInPath()
    {
        // Azure CLI installer puts az.cmd here on Windows but doesn't always update the
        // PATH of already-running processes.  Patch the current process PATH once.
        string[] candidates =
        [
            @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin",
            @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin",
        ];
        var current = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) &&
                !current.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", current + ";" + candidate);
                break;
            }
        }
    }

    private static string? TryGetStringCaseInsensitive(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.String)
            {
                return prop.Value.GetString();
            }
        }
        return null;
    }
}
