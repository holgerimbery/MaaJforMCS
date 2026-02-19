# Copilot Studio Test Runner

An enterprise-grade **multi-agent aware** .NET application for automated testing of Microsoft Copilot Studio agents. Test multiple agents simultaneously across different environments, compare performance, and track quality metrics. Connect via Direct Line, evaluate responses using AI Foundry models, generate test cases from documents, and gain comprehensive insights with detailed reporting.


## Pictures
![home page](./assets/home.jpg)
![wizard](./assets/wizard.jpg)
![test suites](./assets/test-suites.jpg)
![create test suite](./assets/create-test-suite.jpg)
![upload documents - data sources](./assets/upload-documents.jpg)
![create agent](./assets/agent-creation.jpg)
![test run dashboard](./assets/dashboard.jpg)
![test run result](./assets/testrun.jpg)



## Features

- **Multi-Agent Testing**: Configure and test multiple Copilot Studio agents simultaneously
  - Independent configuration per agent (Direct Line, Judge settings, thresholds)
  - Cross-environment testing (dev, staging, production)
  - Side-by-side performance comparison
  - Agent-specific or global question generation settings
- **Direct Line Integration**: Connect to Copilot Studio agents via WebSocket or polling
- **Model-as-a-Judge Evaluation**: Use Azure AI Foundry LLM endpoints to score responses
- **Document-driven Test Generation**: Auto-generate test cases from uploaded PDFs and text files
- **Comprehensive Web UI**: Dashboard, agent management, configuration, transcript viewer, and reporting
- **Setup Wizard**: Guided first-run setup with agent and test suite creation
- **CLI for CI/CD**: Integrate testing into deployment pipelines
- **Local-First Architecture**: Runs entirely locally with external calls only to Direct Line and AI Foundry
- **Rich Metrics**: Pass rates, latency analysis, trend tracking across agents

## Multi-Agent Architecture

The tool is designed from the ground up to support testing multiple Copilot Studio agents in parallel:

### Agent Management
- **Agent Registry**: Define multiple agents with unique configurations
- **Environment Support**: Tag agents by environment (dev, staging, production)
- **Per-Agent Configuration**: Each agent has its own:
  - Direct Line credentials (bot ID, secret, WebSocket preferences)
  - Judge evaluation settings (endpoint, API key, model, temperature, thresholds)
  - Question generation settings (optional overrides, or use global defaults)
  - Timeout and retry policies

### Multi-Agent Test Execution
- **Suite-Agent Mapping**: Associate test suites with one or multiple agents
- **Parallel Execution**: Run the same test suite against multiple agents concurrently
- **Coordinated Scheduling**: Built-in execution coordinator manages rate limits and concurrency
- **Per-Agent Results**: Track success rates, latency, and quality metrics separately for each agent

### Use Cases
- **Cross-Environment Testing**: Run identical test suites against dev, staging, and production agents
- **A/B Testing**: Compare two agent configurations side-by-side
- **Regional Deployment**: Test agents deployed in different geographic regions
- **Version Comparison**: Validate new agent versions against baseline behavior

## Architecture

### Projects

- **CopilotStudioTestRunner.Domain**: Domain entities and configuration models
- **CopilotStudioTestRunner.Data**: EF Core DbContext and database layer (SQLite)
- **CopilotStudioTestRunner.Core**: Core services (Judge, Execution, Document Processing)
- **CopilotStudioTestRunner.WebUI**: ASP.NET Core Blazor Server web interface
- **CopilotStudioTestRunner.CLI**: Command-line interface for CI/CD integration

### Technologies

- **.NET 9**: Latest framework with performance improvements
- **ASP.NET Core**: Web framework with Blazor Server for interactive UI
- **Entity Framework Core**: ORM for SQLite database
- **Azure.AI.OpenAI SDK**: Integration with Azure AI Foundry
- **Serilog**: Structured logging
- **UglyToad.PdfPig**: PDF text extraction
- **Lucene.NET**: Full-text search for documents
- **System.CommandLine**: Modern CLI argument parsing

## Getting Started

### Prerequisites

- .NET 9 SDK
- Azure AI Foundry endpoint (or OpenAI-compatible endpoint) for judge evaluation
- One or more Copilot Studio Web Channel secrets and bot IDs (from the Copilot Studio agent URI)

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd MaaJforMCS

# Build the solution
dotnet build --configuration Release
```

### Configuration

1. **Use the Setup Wizard** in the Web UI to create your first agent and test suite.
2. Add additional agents through the Agents page as needed.
3. All configuration is stored in the SQLite database (no manual config file editing required).

### Running the Application

#### Web UI

```bash
cd CopilotStudioTestRunner.WebUI\bin\Debug\net9.0
CopilotStudioTestRunner.WebUI.exe
# Open the URL shown in the console output
```

#### CLI

```bash
cd CopilotStudioTestRunner.CLI
dotnet run -- run --suite "My Test Suite" --output ./results
dotnet run -- list  # List all suites
```

## Usage

### Web UI

1. **Dashboard**: View summary metrics, recent runs, and quick actions
2. **Agents**: Create, configure, and manage multiple Copilot Studio agents
   - Configure Direct Line credentials per agent
   - Set agent-specific Judge evaluation settings and thresholds
   - Define question generation preferences
   - Tag by environment (dev, staging, production)
3. **Test Suites**: Create, edit, and manage test case collections
   - Associate suites with one or multiple agents
   - Define test cases manually or generate from documents
4. **Documents**: Upload PDFs/text files and auto-generate test cases
5. **Setup Wizard**: Run guided setup for creating your first agent and test suite
6. **Runs**: Browse past runs, view transcripts, compare results across agents

### Setup Wizard

Use the wizard on first run (or from the Web UI) to:

1. Create your first agent with:
   - Agent name and environment tag
   - Direct Line Web Channel secret and bot ID
   - AI Foundry endpoint, deployment name, and API key
   - Judge evaluation thresholds
2. Create your first test suite with initial test cases
3. Start testing immediately or add more agents

### CLI

```bash
# List available suites
dotnet run -- list

# Run a test suite
dotnet run -- run --suite "Regression Tests" --output ./results

# Dry-run (show what would execute)
dotnet run -- run --suite "My Suite" --dry-run
```

Exit codes:
- `0`: All tests passed
- `1`: One or more tests failed or error occurred

## Test Case Definition

Test cases include:

- **User Input**: Multi-step conversation prompts
- **Expected Intent**: Intended user goal
- **Expected Entities**: Domain-specific entities
- **Acceptance Criteria**: Pass/fail requirements
- **Reference Answer**: Optional expected response text
- **Priority/Category**: For filtering and organization

## Evaluation Dimensions

The AI judge scores responses on:

- **Task Success** (0-1): Did the agent complete the request?
- **Intent Match** (0-1): Does response match expected intent?
- **Factuality** (0-1): Is information accurate?
- **Helpfulness** (0-1): Is the response complete and useful?
- **Safety** (0-1): Adherence to safety policies

Configurable weights determine overall pass/fail verdict.

## Document Processing

1. **Upload**: PDF or plain text files
2. **Extract**: Automatic text extraction (PDF via PdfPig)
3. **Chunk**: Sliding window chunking for semantic coherence
4. **Index**: BM25 indexing via Lucene.NET
5. **Generate**: LLM creates Q&A and conversation scripts

## Database

SQLite database with schema for:
- **Agents**: Agent configurations with Direct Line and Judge settings
- **Test Suites and Runs**: Test suite definitions and execution history
- **Suite-Agent Mappings**: Associations between test suites and agents
- **Test Cases and Results**: Individual test cases and their outcomes
- **Transcripts and Messages**: Full conversation logs
- **Documents and Chunks**: Uploaded documents and indexed content
- **Judge and Question Generation Settings**: Global and per-agent configurations

## Troubleshooting

### Direct Line Connection Issues
- Verify bot ID and secret
- Check firewall/proxy rules
- Review Serilog output in `logs/` directory

### Judge Evaluation Failures
- Confirm Azure AI Foundry endpoint URL format
- Verify API key has correct permissions
- Check model name matches deployment

### Database Issues
- Ensure `./data/` directory is writable
- Delete `./data/app.db` to reset (loses all data)
- Run `dotnet ef database update` if migrating

## API Reference

### Agents
- `GET /api/agents` - List all agents
- `GET /api/agents/{id}` - Get agent details
- `POST /api/agents` - Create agent
- `PUT /api/agents/{id}` - Update agent
- `DELETE /api/agents/{id}` - Delete agent

### Test Suites
- `GET /api/testsuites` - List all suites
- `GET /api/testsuites/{id}` - Get suite details
- `POST /api/testsuites` - Create suite
- `PUT /api/testsuites/{id}` - Update suite
- `DELETE /api/testsuites/{id}` - Delete suite

### Runs
- `GET /api/runs` - List all runs
- `GET /api/runs/{id}` - Get run details
- `POST /api/runs` - Start new run
- `GET /api/runs/{id}/results` - Get results

### Results
- `GET /api/results/{id}/transcript` - Get full transcript

### Documents
- `GET /api/documents` - List documents
- `POST /api/documents` - Upload document
- `DELETE /api/documents/{id}` - Delete document

### Metrics
- `GET /api/metrics/summary` - Dashboard summary

## Recent Changes: Multi-Agent Support

The application has been enhanced with comprehensive multi-agent capabilities:

### Key Changes Implemented

1. **Agent Entity & Database Schema**
   - New `Agent` entity with complete configuration (Direct Line, Judge settings, question generation)
   - `TestSuiteAgent` many-to-many relationship enabling suite-agent associations
   - `Run` entity now tracks which agent was tested
   - Migration support for upgrading existing databases

2. **Agent Configuration Service**
   - Centralized service for retrieving agent-specific configurations
   - Support for per-agent Judge settings or fallback to global defaults
   - Per-agent question generation settings with global fallback
   - Runtime configuration resolution

3. **Multi-Agent Execution Coordinator**
   - Parallel test execution across multiple agents
   - Rate limiting and concurrency control per agent
   - Audit logging for multi-agent test runs
   - Error isolation (one agent failure doesn't stop others)

4. **Enhanced Web UI**
   - Agent management pages (list, create, edit, delete)
   - Updated setup wizard for agent-first configuration
   - Run history shows which agent was tested
   - Agent selection when creating/running test suites

5. **Test Data Seeder**
   - Sample multi-agent data for testing and demonstration
   - Multiple sample agents (production, staging, development)
   - Pre-configured test suites with agent associations

### Migration from Single-Agent Version

If upgrading from a previous version:
1. The database will migrate automatically on first run
2. Existing test suites can be associated with newly created agents
3. Global Judge settings are preserved and can be overridden per agent
4. No breaking changes to existing APIs (agents are optional parameters)

## Authentication

The application ships with authentication **disabled by default** so it runs out of the box locally. When deployed for team use, enable Microsoft Entra ID (Azure AD) sign-in to protect all pages and API endpoints with role-based access control.

Three roles are defined:

| Role | Access |
|------|--------|
| **Admin** | Full access — settings, agents, delete operations |
| **Tester** | Create/run test suites, upload documents, view results |
| **Viewer** | Read-only access to dashboards and results |

### Step 1 — Create an Entra ID App Registration

1. Sign in to the [Microsoft Entra admin center](https://entra.microsoft.com).
2. Navigate to **Identity → Applications → App registrations → + New registration**.
3. Enter the following:

   | Field | Value |
   |-------|-------|
   | Name | `CopilotStudioTestRunner` |
   | Supported account types | *Accounts in this organizational directory only* |
   | Redirect URI | Platform: **Web** — URI: `https://localhost:7037/signin-oidc` |

4. Click **Register**.

### Step 2 — Copy the IDs

From the **Overview** page, note down:

| Value | Used as |
|-------|---------|
| **Application (client) ID** | `AZUREAD__CLIENTID` |
| **Directory (tenant) ID** | `AZUREAD__TENANTID` |

### Step 3 — Create a Client Secret

1. Go to **Certificates & secrets → Client secrets → + New client secret**.
2. Enter a description and choose an expiry (12 or 24 months recommended).
3. Click **Add** and **copy the secret value immediately** — it is shown only once.
4. This value becomes `AZUREAD__CLIENTSECRET`.

### Step 4 — Configure Authentication Settings

1. Go to **Authentication**.
2. Under **Redirect URIs**, add all URIs relevant to your deployment:

   | Environment | Redirect URI |
   |-------------|--------------|
   | Local HTTPS | `https://localhost:7037/signin-oidc` |
   | Local HTTP | `http://localhost:5062/signin-oidc` |
   | Production | `https://<your-production-host>/signin-oidc` |

3. Set **Front-channel logout URL** to `https://localhost:7037/signout-oidc` (adjust for production).
4. Under **Implicit grant and hybrid flows**, enable ✅ **ID tokens** and ✅ **Access tokens**.
5. Click **Save**.

### Step 5 — Create App Roles

1. Go to **App roles → + Create app role** and create all three roles:

   | Display name | Value (exact) | Allowed member types |
   |---|---|---|
   | Admin | `Admin` | Users/Groups |
   | Tester | `Tester` | Users/Groups |
   | Viewer | `Viewer` | Users/Groups |

### Step 6 — Assign Users to Roles

1. Go to **Identity → Applications → Enterprise applications → CopilotStudioTestRunner**.
2. Navigate to **Users and groups → + Add user/group**.
3. Select a user or group, pick a role, click **Assign**.
4. Recommended: under **Properties**, set **User assignment required?** to **Yes** so only assigned users can sign in.

### Step 7 — Enable Authentication in the App

Set the following environment variables before starting the application. **Never put secrets in `appsettings.json`.**

**Using PowerShell (local dev):**

```powershell
$env:AZUREAD__TENANTID      = "<Directory (tenant) ID>"
$env:AZUREAD__CLIENTID      = "<Application (client) ID>"
$env:AZUREAD__CLIENTSECRET  = "<Client secret value>"
$env:AUTHENTICATION__ENABLED = "true"
dotnet run --project CopilotStudioTestRunner.WebUI
```

**Using `dotnet user-secrets` (recommended for local dev):**

```bash
cd CopilotStudioTestRunner.WebUI
dotnet user-secrets set "AzureAd:TenantId"      "<tenant-id>"
dotnet user-secrets set "AzureAd:ClientId"      "<client-id>"
dotnet user-secrets set "AzureAd:ClientSecret"  "<client-secret>"
dotnet user-secrets set "Authentication:Enabled" "true"
```

**Using environment variables in Docker:**

```bash
docker run -d \
  -e AZUREAD__TENANTID=<tenant-id> \
  -e AZUREAD__CLIENTID=<client-id> \
  -e AZUREAD__CLIENTSECRET=<client-secret> \
  -e AUTHENTICATION__ENABLED=true \
  copilot-test-runner:latest
```

### How It Works

When `Authentication:Enabled` is `false` (the default), the app uses a local development handler that automatically signs in every request as an **Admin** — no Entra ID required.

When `Authentication:Enabled` is `true`, the app uses OpenID Connect to redirect users to the Microsoft login page. After sign-in, the user's App Role claims (`Admin`, `Tester`, or `Viewer`) control what they can see and do:

- The **Settings** page and **Agent management** are only visible to Admins
- Write operations (create/run test suites, upload documents) require Tester or Admin
- Delete operations require Admin
- All API endpoints enforce the same restrictions
- The `/health` endpoint always remains unauthenticated for container health checks

---

## Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## License
MIT 2026 Holger Imbery

## Support

For issues or questions:
- Check the troubleshooting section above
- Review logs in `logs/` directory
