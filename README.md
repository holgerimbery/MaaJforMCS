# Copilot Studio Test Runner

An enterprise-grade .NET application for automated testing of Microsoft Copilot Studio agents. Connect via Direct Line, evaluate responses using AI Foundry models, generate test cases from documents, and track performance with detailed reporting.


## Pictures
![](./assets/home.jpg)
![](./assets/dashboard.jpg)
![](./assets/testrun.jpg)



## Features

- **Direct Line Integration**: Connect to Copilot Studio agents via WebSocket or polling
- **Model-as-a-Judge Evaluation**: Use Azure AI Foundry LLM endpoints to score responses
- **Document-driven Test Generation**: Auto-generate test cases from uploaded PDFs and text files
- **Comprehensive Web UI**: Dashboard, configuration, transcript viewer, and reporting
- **Setup Wizard**: Guided first-run setup that writes `appsettings.json`
- **CLI for CI/CD**: Integrate testing into deployment pipelines
- **Local-First Architecture**: Runs entirely locally with external calls only to Direct Line and AI Foundry
- **Rich Metrics**: Pass rates, latency analysis, trend tracking

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
- Azure AI Foundry endpoint (or OpenAI-compatible endpoint)
- Copilot Studio Web Channel secret and bot ID (from the Copilot Studio agent URI)

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd MaaJforMCS

# Build the solution
dotnet build --configuration Release
```

### Configuration

1. **Use the Setup Wizard** in the Web UI to enter Direct Line and AI Judge settings.
   The wizard generates the `appsettings.json` file for you.

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
2. **Test Suites**: Create, edit, and manage test case collections
3. **Documents**: Upload PDFs/text files and auto-generate test cases
4. **Setup Wizard**: Run guided setup for Direct Line and AI Judge settings
5. **Settings**: Configure Direct Line, AI Judge, and execution parameters
6. **Runs**: Browse past runs, view transcripts, and compare results

### Setup Wizard

Use the wizard on first run (or from the Web UI) to:

1. Enter Direct Line Web Channel secret and bot ID
2. Enter AI Foundry endpoint, deployment name, and API key
3. Save settings to `appsettings.json`

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
- Test Suites and Runs
- Test Cases and Results
- Transcripts and Messages
- Documents and Chunks
- Judge Settings

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
