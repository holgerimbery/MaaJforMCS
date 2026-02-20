![version](https://img.shields.io/github/v/release/holgerimbery/MaaJforMCS)


<p align="center">
  <img src="assets/maaj-logo.png" width="220" alt="MaaJ Logo">
</p>


# Copilot Studio Test Runner - Multi‑Agent Assessment & Judgement

An enterprise-grade, **multi-agent aware** .NET 9 application for automated testing of Microsoft Copilot Studio agents.
Test multiple agents simultaneously across environments, evaluate responses with Azure AI Foundry models, generate test cases from documents, and gain comprehensive quality metrics

> **Full documentation is available in the [Wiki](https://github.com/holgerimbery/MaaJforMCS/wiki).**

---

## Key Features

- **Multi-Agent Testing** — run the same test suite against dev, staging, and production agents in parallel
- **Environment & Agent Discovery** — browse all Power Platform environments and import Copilot Studio agents automatically via Azure CLI or service principal
- **Direct Line Integration** — WebSocket or polling transport with full conversation lifecycle management
- **Model-as-a-Judge Evaluation** — Azure AI Foundry LLM scores responses on 5 dimensions (task success, intent match, factuality, helpfulness, safety)
- **Document-Driven Test Generation** — upload PDFs or text files and let AI generate test cases automatically
- **Setup Wizard** — guided first-run agent and test suite creation
- **CLI for CI/CD** — exit codes, JSON output, dry-run support
- **Microsoft Entra ID Authentication** — optional enterprise SSO with Admin / Tester / Viewer RBAC roles
- **Backup & Restore** — download a full database snapshot and restore from the Settings page (Admin only)
- **Local-First & Container-Ready** — runs entirely on-premise or in a container via Docker Compose; only calls Direct Line and an AI Foundry endpoint

---

## Screenshots

![Home page](./assets/home.jpg)
![Setup wizard](./assets/wizard.jpg)
![Test suites](./assets/test-suites.jpg)
![Create test suite](./assets/create-test-suite.jpg)
![Upload documents](./assets/upload-documents.jpg)
![Agent creation](./assets/agent-creation.jpg)
![Test run dashboard](./assets/dashboard.jpg)
![Test run result](./assets/testrun.jpg)

---

## Quick Start

```bash
# Clone and build
git clone <repository-url>
cd MaaJforMCS
dotnet build

# Start the Web UI
cd CopilotStudioTestRunner.WebUI
dotnet run
# Open http://localhost:5062 — the Setup Wizard launches automatically
```

The wizard guides you through creating your first agent, uploading documents, generating test cases, and running your first suite.

For a step-by-step walkthrough see [Quick Start](./docs/wiki/Quick-Start.md).

---

## Docker Deployment

Docker support is fully implemented. :white_check_mark:

```bash
# Build and start (all configuration via .env)
docker compose up -d
```

Create a `.env` file from the template below — authentication is controlled by a single flag, no override file required:

```dotenv
JUDGE_ENDPOINT=https://your-resource.openai.azure.com/
JUDGE_API_KEY=your-api-key-here
JUDGE_MODEL=gpt-4o

# Set to true and fill in the Azure AD values to enable Entra ID authentication
AUTHENTICATION_ENABLED=false
AZURE_TENANT_ID=
AZURE_CLIENT_ID=
AZURE_CLIENT_SECRET=
```

Full details including Kubernetes deployment in [Docker Deployment](./docs/wiki/Docker-Deployment.md).

---

## Documentation

| Topic | Link |
|-------|------|
| Getting Started | [Getting-Started](./docs/wiki/Getting-Started.md) |
| Quick Start (5 min) | [Quick-Start](./docs/wiki/Quick-Start.md) |
| Setup Wizard | [Setup-Wizard](./docs/wiki/Setup-Wizard.md) |
| Environment & Agent Discovery | [Environment-Discovery](./docs/wiki/Environment-Discovery.md) |
| Multi-Agent Testing | [Multi-Agent-Testing](./docs/wiki/Multi-Agent-Testing.md) |
| Architecture | [Architecture](./docs/wiki/Architecture.md) |
| Configuration Reference | [Configuration-Reference](./docs/wiki/Configuration-Reference.md) |
| Authentication & RBAC | [Authentication](./docs/wiki/Authentication.md) · [Entra ID Setup](./docs/wiki/Entra-ID-Setup.md) · [RBAC and Roles](./docs/wiki/RBAC-and-Roles.md) |
| Judge Evaluation | [Judge-Evaluation](./docs/wiki/Judge-Evaluation.md) |
| Document Processing | [Document-Processing](./docs/wiki/Document-Processing.md) |
| Test Suites & Cases | [Test-Suites-and-Cases](./docs/wiki/Test-Suites-and-Cases.md) |
| CLI Reference | [CLI-Reference](./docs/wiki/CLI-Reference.md) |
| API Reference | [API-Reference](./docs/wiki/API-Reference.md) |
| Docker Deployment | [Docker-Deployment](./docs/wiki/Docker-Deployment.md) |
| Backup & Restore | [Backup-Restore](./docs/wiki/Backup-Restore.md) |
| Troubleshooting | [Troubleshooting](./docs/wiki/Troubleshooting.md) |

---

## Contributing

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

---

## License

MIT 2026 Holger Imbery
