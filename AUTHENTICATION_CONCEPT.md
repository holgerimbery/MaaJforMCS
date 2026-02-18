# Authentication & Authorization Concept
## Copilot Studio Test Runner — WebUI & API

**Date:** February 18, 2026  
**Version:** 1.0  
**Status:** Concept Phase

---

## Executive Summary

This document defines the authentication and authorization strategy for the Copilot Studio Test Runner web application. The solution leverages **Microsoft Entra ID (Azure AD)** as the identity provider via **OpenID Connect (OIDC)**, providing enterprise-grade SSO for the Blazor Server WebUI and token-based protection for the REST API layer. A role-based access control (RBAC) model ensures least-privilege access across three defined roles.

---

## 1. Goals & Requirements

### Functional Requirements

| ID | Requirement |
|----|------------|
| FR-1 | Users must authenticate before accessing the WebUI or API |
| FR-2 | Support Single Sign-On (SSO) via Microsoft Entra ID |
| FR-3 | Role-based access: **Admin**, **Tester**, **Viewer** |
| FR-4 | API endpoints must be protected with bearer token authentication |
| FR-5 | Unauthenticated requests are redirected to the login page (UI) or receive `401` (API) |
| FR-6 | User identity is recorded in audit fields (`CreatedBy`, `ExecutionUser`) |
| FR-7 | Session timeout after configurable period of inactivity |

### Non-Functional Requirements

| ID | Requirement |
|----|------------|
| NFR-1 | No credentials stored in the application database |
| NFR-2 | All auth traffic over HTTPS/TLS |
| NFR-3 | Token validation performed server-side |
| NFR-4 | Compatible with Docker and Kubernetes deployment |
| NFR-5 | Graceful fallback for development/local scenarios |

---

## 2. Identity Provider: Microsoft Entra ID

### Why Entra ID

- The application already integrates with Azure services (AI Foundry, Direct Line)
- Enterprise customers expect Entra ID integration
- Provides OIDC, OAuth 2.0, and SAML protocols
- Built-in MFA, Conditional Access, and audit logging
- App Roles feature maps directly to RBAC needs

### App Registration — Step-by-Step

Follow these steps to create and configure the Entra ID App Registration.

#### Step 1: Create the App Registration

1. Sign in to the [Microsoft Entra admin center](https://entra.microsoft.com) (or the Azure Portal → **Microsoft Entra ID**).
2. Navigate to **Identity → Applications → App registrations**.
3. Click **+ New registration**.
4. Fill in the form:

   | Field | Value |
   |-------|-------|
   | **Name** | `CopilotStudioTestRunner` |
   | **Supported account types** | *Accounts in this organizational directory only* (single tenant). Choose *Accounts in any organizational directory* if you need multi-tenant access. |
   | **Redirect URI** | Platform: **Web** — URI: `https://<your-host>/signin-oidc` (e.g., `https://localhost:5001/signin-oidc` for local dev) |

5. Click **Register**.

#### Step 2: Note the IDs

After registration, the **Overview** page shows:

| Value | Where to use |
|-------|-------------|
| **Application (client) ID** | `AzureAd:ClientId` in appsettings.json |
| **Directory (tenant) ID** | `AzureAd:TenantId` in appsettings.json |

#### Step 3: Create a Client Secret

1. In the App Registration, go to **Certificates & secrets → Client secrets**.
2. Click **+ New client secret**.
3. Enter a description (e.g., `WebUI Production`) and choose an expiry (recommended: 12 or 24 months).
4. Click **Add**.
5. **Copy the secret value immediately** — it is only shown once. Store it in `AzureAd:ClientSecret` (via environment variable or secrets manager, never in source control).

#### Step 4: Configure Authentication Settings

1. Go to **Authentication**.
2. Under **Web → Redirect URIs**, verify `https://<your-host>/signin-oidc` is listed. Add additional URIs for other environments (e.g., `https://localhost:5001/signin-oidc`).
3. Set **Front-channel logout URL** to `https://<your-host>/signout-oidc`.
4. Under **Implicit grant and hybrid flows**, check:
   - ✅ **ID tokens** (required for OIDC sign-in)
   - ✅ **Access tokens** (required for API bearer auth)
5. Click **Save**.

#### Step 5: Define App Roles

1. Go to **App roles**.
2. Click **+ Create app role** and create each of the three roles:

   | Display name | Value | Description | Allowed member types |
   |-------------|-------|-------------|---------------------|
   | `Admin` | `Admin` | Full access: manage settings, agents, test suites, run tests, view results | Users/Groups |
   | `Tester` | `Tester` | Run tests, manage test suites and documents, view results | Users/Groups |
   | `Viewer` | `Viewer` | Read-only access to dashboards, runs, and results | Users/Groups |

3. Verify each role shows **Enabled = Yes**.

> **Alternative:** You can also edit the manifest JSON directly under **Manifest** and add the `appRoles` array shown below.

#### Step 6: Assign Users to Roles

1. Navigate to **Identity → Applications → Enterprise applications**.
2. Find and select **CopilotStudioTestRunner**.
3. Go to **Users and groups → + Add user/group**.
4. Select a user or group, then select a role (Admin, Tester, or Viewer).
5. Click **Assign**.
6. Repeat for all users who need access.

> **Note:** If *User assignment required?* (under **Properties**) is set to **Yes**, only users explicitly assigned a role can sign in. This is recommended for production.

#### Step 7: Configure API Permissions (Optional)

If the CLI needs to call the API using client credentials:

1. Go to **API permissions → + Add a permission → My APIs**.
2. Select `CopilotStudioTestRunner`.
3. Choose **Application permissions** and select the roles you want the CLI service principal to have.
4. Click **Grant admin consent for \<tenant\>**.

#### Step 8: Expose an API (Optional, for CLI/Service-to-Service)

1. Go to **Expose an API**.
2. Set the **Application ID URI** (e.g., `api://<client-id>`).
3. Click **+ Add a scope** with:
   - Scope name: `access_as_user`
   - Who can consent: Admins and users
   - Admin consent display name: `Access CopilotStudioTestRunner`
4. Click **Add scope**.

#### Reference: Final Configuration Values

After completing the steps above, add these values to your environment or appsettings:

```
AZUREAD__TENANTID=<Directory (tenant) ID from Step 2>
AZUREAD__CLIENTID=<Application (client) ID from Step 2>
AZUREAD__CLIENTSECRET=<Secret value from Step 3>
AUTHENTICATION__ENABLED=true
```

#### Reference: App Roles Manifest JSON

If editing the manifest directly, replace the `appRoles` array with:

```json
"appRoles": [
  {
    "allowedMemberTypes": ["User"],
    "displayName": "Admin",
    "description": "Full access: manage settings, agents, test suites, run tests, view results",
    "value": "Admin",
    "isEnabled": true
  },
  {
    "allowedMemberTypes": ["User"],
    "displayName": "Tester",
    "description": "Run tests, manage test suites and documents, view results",
    "value": "Tester",
    "isEnabled": true
  },
  {
    "allowedMemberTypes": ["User"],
    "displayName": "Viewer",
    "description": "Read-only access to dashboards, runs, and results",
    "value": "Viewer",
    "isEnabled": true
  }
]
```

Users/groups are assigned to roles via **Enterprise Applications → Users and groups** in the Azure Portal.

---

## 3. Authentication Flow

### 3.1 Blazor Server (WebUI) — OIDC Authorization Code Flow

```
┌───────────┐       ┌──────────────────┐       ┌──────────────────┐
│  Browser  │       │  Blazor Server   │       │  Microsoft       │
│  (User)   │       │  (ASP.NET Core)  │       │  Entra ID        │
└─────┬─────┘       └────────┬─────────┘       └────────┬─────────┘
      │  1. GET /dashboard         │                      │
      │───────────────────────────>│                      │
      │                            │  2. Not authenticated │
      │  3. 302 Redirect to Entra  │                      │
      │<───────────────────────────│                      │
      │                            │                      │
      │  4. Login at Entra ID      │                      │
      │──────────────────────────────────────────────────>│
      │                            │                      │
      │  5. Auth code callback     │                      │
      │───────────────────────────>│                      │
      │                            │  6. Exchange code     │
      │                            │  for tokens           │
      │                            │─────────────────────>│
      │                            │                      │
      │                            │  7. ID token +        │
      │                            │  access token         │
      │                            │<─────────────────────│
      │                            │                      │
      │  8. Authenticated session  │  9. Validate token,   │
      │  (cookie-based)            │  extract roles        │
      │<───────────────────────────│                      │
```

**Key points:**
- ASP.NET Core cookie authentication maintains the server-side session
- The ID token provides user identity and role claims
- Blazor Server uses `AuthenticationStateProvider` to propagate claims to components
- SignalR circuit inherits the authenticated session

### 3.2 REST API — Bearer Token Authentication

For programmatic/CLI access, the API accepts **OAuth 2.0 Bearer tokens**:

```
┌───────────┐       ┌──────────────────┐       ┌──────────────────┐
│  CLI /    │       │  ASP.NET Core    │       │  Microsoft       │
│  Client   │       │  API             │       │  Entra ID        │
└─────┬─────┘       └────────┬─────────┘       └────────┬─────────┘
      │  1. Acquire token     │                          │
      │  (client credentials  │                          │
      │   or device code)     │                          │
      │─────────────────────────────────────────────────>│
      │                       │                          │
      │  2. Access token      │                          │
      │<─────────────────────────────────────────────────│
      │                       │                          │
      │  3. GET /api/runs     │                          │
      │  Authorization: Bearer <token>                   │
      │──────────────────────>│                          │
      │                       │  4. Validate token       │
      │                       │  (signature, audience,   │
      │                       │   issuer, expiry)        │
      │                       │                          │
      │  5. 200 OK + data     │                          │
      │<──────────────────────│                          │
```

**Supported grant types:**
| Grant Type | Use Case |
|------------|----------|
| Authorization Code + PKCE | Interactive CLI sessions |
| Client Credentials | CI/CD pipelines, automation |
| Device Code | Headless environments |

---

## 4. Authorization Model (RBAC)

### 4.1 Role Definitions

| Role | Description | Typical Users |
|------|-------------|---------------|
| **Admin** | Full system control including settings, agent configuration, and user management | Team leads, platform owners |
| **Tester** | Create/edit test suites, upload documents, execute test runs, view results | QA engineers, developers |
| **Viewer** | Read-only access to dashboards, runs, results, and transcripts | Stakeholders, management |

### 4.2 Permission Matrix

| Resource / Action | Admin | Tester | Viewer |
|-------------------|:-----:|:------:|:------:|
| **Dashboard** | ✅ | ✅ | ✅ |
| View metrics summary | ✅ | ✅ | ✅ |
| **Test Suites** | | | |
| List / View | ✅ | ✅ | ✅ |
| Create | ✅ | ✅ | ❌ |
| Edit | ✅ | ✅ | ❌ |
| Delete | ✅ | ❌ | ❌ |
| **Test Cases** | | | |
| List / View | ✅ | ✅ | ✅ |
| Create / Edit | ✅ | ✅ | ❌ |
| Delete | ✅ | ❌ | ❌ |
| **Runs** | | | |
| List / View | ✅ | ✅ | ✅ |
| Start execution | ✅ | ✅ | ❌ |
| View results / transcripts | ✅ | ✅ | ✅ |
| **Documents** | | | |
| List / View | ✅ | ✅ | ✅ |
| Upload | ✅ | ✅ | ❌ |
| Delete | ✅ | ❌ | ❌ |
| **Agents** | | | |
| List / View | ✅ | ✅ | ✅ |
| Create / Edit | ✅ | ❌ | ❌ |
| Delete | ✅ | ❌ | ❌ |
| **Settings** | | | |
| View configuration | ✅ | ❌ | ❌ |
| Edit configuration | ✅ | ❌ | ❌ |
| Test Direct Line connection | ✅ | ✅ | ❌ |
| **Audit Logs** | | | |
| View | ✅ | ❌ | ❌ |

### 4.3 Role Enforcement Points

Roles are enforced at two levels:

1. **API layer** — `[Authorize(Roles = "Admin,Tester")]` attributes on minimal API endpoints
2. **UI layer** — `<AuthorizeView Roles="Admin">` in Blazor components to conditionally render UI elements

---

## 5. Implementation Plan

### 5.1 NuGet Packages Required

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="9.0.*" />
<PackageReference Include="Microsoft.Identity.Web" Version="3.*" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="3.*" />
```

### 5.2 Configuration (appsettings.json)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "${AZURE_TENANT_ID}",
    "ClientId": "${AZURE_CLIENT_ID}",
    "ClientSecret": "${AZURE_CLIENT_SECRET}",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "Authentication": {
    "Enabled": true,
    "SessionTimeoutMinutes": 60,
    "RequireHttpsMetadata": true
  }
}
```

> **Security:** `ClientSecret` and `TenantId` must be provided via environment variables or a secrets manager — never committed to source control.

### 5.3 Service Registration (Program.cs)

```csharp
// ── Authentication ──────────────────────────────────────────────
var authEnabled = config.GetValue<bool>("Authentication:Enabled", true);

if (authEnabled)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(config.GetSection("AzureAd"));

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("TesterOrAbove", policy => policy.RequireRole("Admin", "Tester"));
        options.AddPolicy("AnyAuthenticated", policy => policy.RequireAuthenticatedUser());

        // Fallback: require authentication for all endpoints by default
        options.FallbackPolicy = options.DefaultPolicy;
    });
}
else
{
    // Development-only: allow anonymous access
    builder.Services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(
            "Development", null);
    builder.Services.AddAuthorization();
}
```

### 5.4 Middleware Pipeline (Program.cs)

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();    // ← Add before UseAuthorization
app.UseAuthorization();     // ← Add before MapRazorComponents

app.UseAntiforgery();
```

### 5.5 API Endpoint Protection

```csharp
// Read-only endpoints — any authenticated user
api.MapGet("/testsuites", GetTestSuites).RequireAuthorization("AnyAuthenticated");
api.MapGet("/runs", GetRuns).RequireAuthorization("AnyAuthenticated");
api.MapGet("/metrics/summary", GetMetricsSummary).RequireAuthorization("AnyAuthenticated");

// Write endpoints — Tester or Admin
api.MapPost("/testsuites", CreateTestSuite).RequireAuthorization("TesterOrAbove");
api.MapPut("/testsuites/{id}", UpdateTestSuite).RequireAuthorization("TesterOrAbove");
api.MapPost("/runs", StartRun).RequireAuthorization("TesterOrAbove");
api.MapPost("/documents", UploadDocument).RequireAuthorization("TesterOrAbove");
api.MapPost("/test-connection", TestDirectLineConnection).RequireAuthorization("TesterOrAbove");

// Destructive endpoints — Admin only
api.MapDelete("/testsuites/{id}", DeleteTestSuite).RequireAuthorization("AdminOnly");
api.MapDelete("/documents/{id}", DeleteDocument).RequireAuthorization("AdminOnly");
```

### 5.6 Blazor Component Integration

#### App.razor — Add CascadingAuthenticationState

```razor
<CascadingAuthenticationState>
    <Routes @rendermode="InteractiveServer" />
</CascadingAuthenticationState>
```

#### MainLayout.razor — User Info & Logout

```razor
@inject AuthenticationStateProvider AuthState

<!-- In the header bar -->
<AuthorizeView>
    <Authorized>
        <div style="display: flex; align-items: center; gap: 12px;">
            <span>@context.User.Identity?.Name</span>
            <span class="badge bg-secondary">@GetUserRole(context)</span>
            <form method="post" action="/signout-oidc">
                <button type="submit" class="btn btn-outline-secondary btn-sm">Sign out</button>
            </form>
        </div>
    </Authorized>
</AuthorizeView>

@code {
    private string GetUserRole(AuthenticationState context)
    {
        var roles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value);
        return string.Join(", ", roles);
    }
}
```

#### Conditional UI Elements

```razor
<!-- Show "New Test Suite" button only for Tester / Admin -->
<AuthorizeView Roles="Admin,Tester">
    <button class="btn btn-primary" @onclick="CreateNewSuite">
        <i class="bi bi-plus-circle"></i> New Test Suite
    </button>
</AuthorizeView>

<!-- Show Settings nav item only for Admin -->
<AuthorizeView Roles="Admin">
    <div class="nav-button" @onclick="GoSettings" title="Settings">
        <i class="bi bi-gear"></i>
    </div>
</AuthorizeView>
```

### 5.7 Capturing User Identity for Audit

Extract the authenticated user identity in API handlers:

```csharp
async Task<IResult> CreateTestSuite(
    CreateTestSuiteRequest request,
    TestRunnerDbContext db,
    ClaimsPrincipal user)   // ← Injected automatically by ASP.NET Core
{
    var suite = new TestSuite
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description ?? "",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = user.Identity?.Name ?? "unknown"  // ← Use authenticated identity
    };
    // ...
}
```

### 5.8 Development Authentication Handler

For local development without Entra ID:

```csharp
public class DevelopmentAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "dev-user@localhost"),
            new Claim(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

> **Warning:** `DevelopmentAuthHandler` must only be active when `Authentication:Enabled` is `false`. It is never used in production.

---

## 6. Login & Logout Pages

### 6.1 Login Page (Optional Custom)

The default OIDC middleware handles the redirect to Entra ID automatically. Optionally add a branded landing page:

```razor
@page "/login"
@layout EmptyLayout

<div style="display: flex; justify-content: center; align-items: center; height: 100vh; background: linear-gradient(135deg, #0066cc, #0052a3);">
    <div style="background: white; padding: 48px; border-radius: 12px; text-align: center; box-shadow: 0 8px 32px rgba(0,0,0,0.15);">
        <div style="font-size: 48px; color: #0066cc; font-weight: 900; margin-bottom: 16px;">CS</div>
        <h2 style="margin-bottom: 8px;">Copilot Studio Test Runner</h2>
        <p style="color: #666; margin-bottom: 24px;">Sign in with your organizational account</p>
        <a href="/MicrosoftIdentity/Account/SignIn" class="btn btn-primary btn-lg" style="min-width: 200px;">
            <i class="bi bi-microsoft"></i> Sign in with Microsoft
        </a>
    </div>
</div>
```

### 6.2 Access Denied Page

```razor
@page "/access-denied"
@layout EmptyLayout

<div style="display: flex; justify-content: center; align-items: center; height: 100vh;">
    <div style="text-align: center; max-width: 400px;">
        <i class="bi bi-shield-lock" style="font-size: 64px; color: #dc3545;"></i>
        <h2>Access Denied</h2>
        <p>You do not have permission to access this resource. Contact your administrator to request the appropriate role.</p>
        <a href="/" class="btn btn-primary">Return Home</a>
    </div>
</div>
```

---

## 7. Session Management

| Setting | Value | Notes |
|---------|-------|-------|
| Cookie name | `.AspNetCore.Cookies` | Default ASP.NET Core cookie |
| Cookie `HttpOnly` | `true` | Prevents XSS access |
| Cookie `Secure` | `true` | HTTPS only |
| Cookie `SameSite` | `Lax` | CSRF protection |
| Session timeout | 60 minutes (configurable) | Sliding expiration |
| Token refresh | Automatic via MSAL | Refresh tokens handled server-side |

```csharp
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            config.GetValue<int>("Authentication:SessionTimeoutMinutes", 60));
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.AccessDeniedPath = "/access-denied";
    });
```

---

## 8. Security Considerations

### 8.1 Token Validation

All tokens are validated server-side by the `Microsoft.Identity.Web` middleware:

- **Signature** — Verified against Entra ID's public signing keys (JWKS)
- **Issuer** — Must match the configured tenant
- **Audience** — Must match the application's Client ID
- **Expiry** — Expired tokens are rejected
- **Nonce** — Replay attacks prevented

### 8.2 CSRF Protection

- Blazor Server uses `AntiforgeryToken` middleware (already configured via `app.UseAntiforgery()`)
- Sign-out is via POST form to prevent CSRF-based logout attacks
- API endpoints use Bearer tokens (inherently CSRF-safe)

### 8.3 Secrets Management

| Environment | Mechanism |
|-------------|-----------|
| Local development | `dotnet user-secrets` or `Authentication:Enabled = false` |
| Docker | Docker Secrets or environment variables |
| Kubernetes | Kubernetes Secrets (mounted as env vars or files) |
| Azure | Azure Key Vault with managed identity |

### 8.4 CORS Policy

When the API is called cross-origin (e.g., from a separate SPA):

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(config.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### 8.5 Security Headers

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});
```

---

## 9. Docker & Deployment Considerations

### 9.1 Environment Variables for Auth Configuration

```bash
# Required for authentication
AZUREAD__TENANTID=<your-tenant-id>
AZUREAD__CLIENTID=<your-client-id>
AZUREAD__CLIENTSECRET=<your-client-secret>

# Optional overrides
AUTHENTICATION__ENABLED=true
AUTHENTICATION__SESSIONTIMEOUTMINUTES=60
AUTHENTICATION__REQUIREHTTPSMETADATA=true
```

### 9.2 Reverse Proxy (nginx/Traefik)

For production deployments with TLS termination at the proxy:

```csharp
// Program.cs — trust forwarded headers from the reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

app.UseForwardedHeaders();
```

This ensures the OIDC middleware generates correct redirect URIs when behind a load balancer.

### 9.3 Health Check Exclusion

The `/health` endpoint should remain unauthenticated for container orchestrators:

```csharp
app.MapHealthChecks("/health").AllowAnonymous();
```

---

## 10. CLI Authentication

### 10.1 Interactive (Device Code Flow)

```csharp
// CLI acquires a token via device code for interactive use
var app = PublicClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
    .WithTenantId(tenantId)
    .Build();

var result = await app.AcquireTokenWithDeviceCode(
    scopes: new[] { $"api://{clientId}/.default" },
    deviceCodeResultCallback: code =>
    {
        Console.WriteLine(code.Message);  // "Go to https://microsoft.com/devicelogin and enter code ABC123"
        return Task.CompletedTask;
    }).ExecuteAsync();

// Use result.AccessToken for API calls
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", result.AccessToken);
```

### 10.2 Non-Interactive (Client Credentials)

For CI/CD pipelines, register a second Entra ID app (or use the same with a client secret/certificate):

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithClientSecret(clientSecret)
    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
    .Build();

var result = await app.AcquireTokenForClient(
    scopes: new[] { $"api://{clientId}/.default" })
    .ExecuteAsync();
```

---

## 11. Migration & Rollout Plan

### Phase 1: Foundation (Authentication)
- [ ] Create Entra ID App Registration
- [ ] Add NuGet packages (`Microsoft.Identity.Web`)
- [ ] Implement OIDC authentication in `Program.cs`
- [ ] Add `Authentication` configuration section
- [ ] Create `DevelopmentAuthHandler` for local dev
- [ ] Modify `App.razor` to add `CascadingAuthenticationState`
- [ ] Add login/logout UI to `MainLayout.razor`
- [ ] Add `/login` and `/access-denied` pages
- [ ] Ensure `/health` excluded from auth

### Phase 2: Authorization (RBAC)
- [ ] Define App Roles in Entra ID manifest
- [ ] Add authorization policies in `Program.cs`
- [ ] Apply `RequireAuthorization()` to all API endpoints
- [ ] Add `<AuthorizeView>` to Blazor components
- [ ] Update `CreatedBy` / `ExecutionUser` to use authenticated identity
- [ ] Hide navigation items based on role

### Phase 3: Hardening
- [ ] Add security response headers
- [ ] Configure CORS policy
- [ ] Set up forwarded headers for reverse proxy
- [ ] Add session timeout configuration
- [ ] Implement CLI device code flow
- [ ] Security testing (token replay, privilege escalation, CSRF)
- [ ] Update Docker concept with auth environment variables
- [ ] Update documentation (`QUICKSTART.md`, `DEPLOYMENT.md`)

---

## 12. Testing Strategy

### Unit Tests
- `DevelopmentAuthHandler` returns expected claims
- Authorization policies map roles correctly
- Endpoints reject unauthenticated requests

### Integration Tests
- OIDC flow with test tenant (or mock)
- Role-based endpoint access (Admin can delete, Viewer cannot)
- Token expiry and refresh behavior
- Sign-out clears session completely

### Manual Verification Checklist
- [ ] Unauthenticated user redirected to Entra ID login
- [ ] Successful login returns to the originally requested page
- [ ] User name and role displayed in the header
- [ ] Viewer cannot see "New Test Suite" buttons
- [ ] Viewer gets 403 when calling POST endpoints
- [ ] Admin can access Settings page
- [ ] Sign-out redirects to login page
- [ ] Expired session redirects to re-authentication
- [ ] `/health` accessible without authentication
- [ ] CLI can authenticate via device code flow

---

## 13. Alternative Approaches Considered

| Approach | Pros | Cons | Decision |
|----------|------|------|----------|
| **Microsoft Entra ID (OIDC)** | Enterprise-ready, SSO, MFA, App Roles | Requires Azure tenant | ✅ Selected |
| **ASP.NET Core Identity (local DB)** | No external dependency | Password management burden, no SSO | ❌ Rejected |
| **API Keys (header-based)** | Simple for CLI/CI | No user identity, hard to rotate | ❌ Rejected (used only as complement) |
| **Auth0 / Okta** | Multi-provider, hosted | Additional cost, external dependency | ❌ Not needed (Entra ID suffices) |
| **Windows Authentication (Kerberos)** | Transparent SSO on domain | Doesn't work in Docker/cloud | ❌ Rejected |

---

## 14. Configuration Reference

### Full `appsettings.json` Auth Section

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "Domain": "yourdomain.onmicrosoft.com"
  },
  "Authentication": {
    "Enabled": true,
    "SessionTimeoutMinutes": 60,
    "RequireHttpsMetadata": true
  }
}
```

### Environment Variable Mapping

| JSON Path | Environment Variable |
|-----------|---------------------|
| `AzureAd:TenantId` | `AZUREAD__TENANTID` |
| `AzureAd:ClientId` | `AZUREAD__CLIENTID` |
| `AzureAd:ClientSecret` | `AZUREAD__CLIENTSECRET` |
| `Authentication:Enabled` | `AUTHENTICATION__ENABLED` |
| `Authentication:SessionTimeoutMinutes` | `AUTHENTICATION__SESSIONTIMEOUTMINUTES` |

---

## Appendix A: Sequence Diagram — Full Login Flow

```
User            Browser         ASP.NET Core        Entra ID
 │                │                  │                  │
 │  Navigate to   │                  │                  │
 │  /dashboard    │                  │                  │
 │───────────────>│  GET /dashboard  │                  │
 │                │─────────────────>│                  │
 │                │                  │ Check auth cookie │
 │                │                  │ (none found)      │
 │                │  302 → /login    │                  │
 │                │<─────────────────│                  │
 │                │                  │                  │
 │                │  302 → Entra ID  │                  │
 │                │  authorize EP    │                  │
 │                │─────────────────────────────────────>│
 │                │                  │                  │
 │  Entra ID      │                  │                  │
 │  login form    │                  │                  │
 │<───────────────│                  │                  │
 │                │                  │                  │
 │  Enter creds   │                  │                  │
 │  (+ MFA)       │                  │                  │
 │───────────────>│                  │                  │
 │                │─────────────────────────────────────>│
 │                │                  │                  │
 │                │  302 → /signin-oidc?code=XYZ        │
 │                │<─────────────────────────────────────│
 │                │                  │                  │
 │                │  POST /signin-oidc                  │
 │                │─────────────────>│                  │
 │                │                  │  POST /token      │
 │                │                  │  (code → tokens)  │
 │                │                  │─────────────────>│
 │                │                  │                  │
 │                │                  │  ID + Access token│
 │                │                  │<─────────────────│
 │                │                  │                  │
 │                │                  │ Validate, create  │
 │                │                  │ ClaimsPrincipal   │
 │                │                  │ Set auth cookie   │
 │                │  302 → /dashboard                   │
 │                │<─────────────────│                  │
 │                │                  │                  │
 │  Dashboard     │  GET /dashboard  │                  │
 │  renders       │  (authenticated) │                  │
 │<───────────────│─────────────────>│                  │
```

## Appendix B: Glossary

| Term | Definition |
|------|-----------|
| **OIDC** | OpenID Connect — identity layer on top of OAuth 2.0 |
| **MSAL** | Microsoft Authentication Library — handles token acquisition/caching |
| **App Role** | Entra ID feature to define application-specific roles |
| **Claims** | Key-value pairs in a security token (e.g., name, role) |
| **Bearer Token** | An OAuth 2.0 access token sent in the `Authorization` header |
| **PKCE** | Proof Key for Code Exchange — prevents authorization code interception |
| **Sliding Expiration** | Session timeout resets on each request |

---

**End of Authentication Concept Document**

Ready for review and approval to proceed with implementation.
