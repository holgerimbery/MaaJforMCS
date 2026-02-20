# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Conversation flow testing — multi-turn test cases across several exchanges
- Test case import from CSV/Excel — bulk upload of questions
- Question generation wizard — guided wizard from an uploaded knowledge base document
- Re-run failed tests only — trigger a new run executing only failed test cases
- Test case cloning — duplicate an existing test case to create variants
- Bulk test case operations — enable, disable, or delete multiple test cases at once
- Test case search & filter within suite — keyword search and filter by category, tag, or active state
- Side-by-side agent comparison — diff of pass rates, latency, and scores between two agents
- Confidence scoring trends — track judge score trends per test case over time
- Latency percentile chart (P50/P95/P99) — visual trend over time
- Topic/tag grouping — tag test cases by topic and show pass rates per topic
- Manual verdict override — allow a tester or admin to override the AI judge's verdict
- Pass threshold per test suite — override global judge pass threshold at the suite level
- Judge prompt customization — per-agent or per-suite customization of the judge system prompt
- Scheduled runs — cron-style scheduling for automatic suite execution
- Webhook / Teams notification — post a summary card to Teams on run completion or regression
- Email notification — send a run-summary email via SMTP
- Run history pruning — configurable retention policy for the SQLite database
- Export run report to PDF/Excel — one-click download of a formatted test report
- Test suite import / export (JSON) — share test suites between environments
- Agent environment filter on runs — filter run history and dashboard by environment label
- Role-based test suite ownership — users can only edit/delete suites they created
- Audit log page — surface the existing `AuditLog` entity in the UI
- REST API key auth — allow CI pipelines to trigger runs without browser login
- OpenTelemetry traces — instrument test execution with trace IDs linkable to Azure Monitor
- CLI: `generate` command — generate test cases from a document entirely from CI
- CLI: `report` command — export a previous run's results as JSON or CSV from CI
- CLI: `agents` command — list configured agents from CLI

## [0.6.0] - 2026-02-20

### Added
- Agent Leaderboard card on Dashboard (replaces static System Information)
- Filter bar on Dashboard → Recent Runs (Suite Name / Agent / Date / Pass Rate)
- Version number display (from `VERSION` file) on Help page
- Runtime Environment card on Help page (container vs bare metal, auth mode, proxy headers)
- Azure CLI disclaimer on Environment Discovery page when running in a container
- Updated image assets for home and wizard pages

### Fixed
- Expose app via external nginx proxy with SSL termination — trust `X-Forwarded-*` headers (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`)
- PowerApps API endpoint for token acquisition in `EnvironmentDiscoveryPage` and `PowerPlatformDiscoveryService` to correctly acquire a scoped Dataverse token for agent loading when running in a container
- Reorder credential chain in `CreateDefaultCredential` for improved local development experience

## [0.5.0] - 2026-02-20

### Added
- Environment & Agent Discovery — browse all Power Platform environments and import Copilot Studio agents automatically via Azure CLI or service principal
- Multi-agent support — run a test suite against multiple agents simultaneously
- Microsoft Entra ID authentication with Admin / Tester / Viewer RBAC roles
- Docker authentication configuration and template
- MaaJ logo asset and updated README branding

### Changed
- Docker configuration updated for multi-agent support and authentication enhancements

## [0.3.0] - 2026-02-19

### Added
- Favicon SVG asset
- Local restart script (`localrestart.ps1`) for development convenience
- Wiki documentation as a git submodule
- Access denied and login pages
- Docker containerization strategy documentation

### Changed
- Authentication handler added for development environment

## [0.2.0] - 2025-xx-xx

### Added
- Multi-agent execution coordinator
- Parallel test execution across multiple agents

## [0.1.0] - 2025-xx-xx

### Added
- Initial release of Copilot Studio Test Runner Web UI
- API endpoints and SQLite database integration
- DirectLine client with WebSocket and polling transport
- Model-as-a-Judge evaluation via Azure AI Foundry (5 dimensions: task success, intent match, factuality, helpfulness, safety)
- Document processing — upload PDFs or text files to generate test cases automatically
- Test suite and test case management (create, edit, clone, import/export)
- Dashboard with run history and metrics
- Setup Wizard for guided first-run configuration
- CLI with exit codes, JSON output, and dry-run support
- Semantic variation generation for test cases
