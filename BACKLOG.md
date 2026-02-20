# Backlog

## Test Quality

- [ ] **Conversation flow testing** — multi-turn test cases that verify the agent handles context across several exchanges, not just single Q&A
- [ ] **Regression detection** — flag when a test that previously passed now fails, with a diff of the judge rationale
- [ ] **Test case import from CSV/Excel** — bulk upload questions instead of entering them one by one
- [ ] **Question generation wizard** — guided wizard that generates test cases from an uploaded knowledge base document
- [ ] **Re-run failed tests only** — from a completed run, trigger a new run that executes only the test cases that failed, without re-running passing ones
- [ ] **Test case cloning** — duplicate an existing test case within a suite to quickly create variants without re-entering data
- [ ] **Bulk test case operations** — select multiple test cases and enable, disable, or delete them in a single action
- [ ] **Test case search & filter within suite** — keyword search and filter by category, tag, or active state inside a suite's test case editor

## Evaluation & Insights

- [ ] **Side-by-side agent comparison** — run the same suite against two agents and show a diff of pass rates, latency and scores
- [ ] **Confidence scoring trends** — track judge score trends per individual test case over time to spot degrading answers
- [ ] **Latency percentile chart (P50/P95/P99)** — visual over time, not just the current average
- [ ] **Topic/tag grouping** — tag test cases by topic and show pass rates per topic to identify weak subject areas
- [ ] **Manual verdict override** — allow a tester or admin to override the AI judge's verdict on a specific result with a human annotation and justification, useful for edge cases where the judge is incorrect
- [ ] **Pass threshold per test suite** — override the global judge pass threshold at the suite level so critical suites can enforce a stricter standard than general-purpose ones
- [ ] **Judge prompt customization** — allow per-agent or per-suite customization of the judge system prompt to tune evaluation criteria for specialized domains (e.g. legal, medical, HR)

## Operations

- [ ] **Scheduled runs** — cron-style scheduling so suites run automatically (e.g. nightly) without manual trigger
- [ ] **Webhook / Teams notification** — post a summary card to a Teams channel when a run completes or a regression is detected
- [ ] **Email notification** — send a run-summary email via SMTP on completion or regression, complementing the Teams webhook for teams that don't use Teams
- [ ] **Run history pruning** — configurable retention policy to keep the SQLite database from growing unbounded
- [ ] **Export run report to PDF/Excel** — one-click download of a formatted test report for sharing with stakeholders
- [ ] **Test suite import / export (JSON)** — export a full suite (including test cases) as a JSON file and import it into another instance to share test suites between environments or teams
- [ ] **Agent environment filter on runs** — filter the run history and dashboard by the agent's environment label (dev / staging / production) which is already stored on the `Agent` entity but not yet surfaced in the UI

## Security & Multi-tenancy

- [ ] **Role-based test suite ownership** — users can only edit/delete suites they created; admins see everything
- [ ] **Audit log page** — surface the existing `AuditLog` entity in the UI

## Developer Experience

- [ ] **REST API key auth** — allow CI pipelines to trigger runs via the existing API without browser login
- [ ] **OpenTelemetry traces** — instrument test execution so each run has a trace ID linkable to Azure Monitor
- [ ] **CLI: `generate` command** — add a `testrunner generate --document <path> --suite <name>` command so teams can generate test cases from a document entirely from CI without opening the web UI
- [ ] **CLI: `report` command** — add a `testrunner report --run <id>` command to export a previous run's results as JSON or CSV from CI without re-running the suite
- [ ] **CLI: `agents` command** — add a `testrunner agents` command to list configured agents, parallel to the existing `list` (suites) command

## Completed

- [x] Publish versioned container image to Docker Hub and ghcr.io via GitHub Actions on release tag, with auto-generated quickstart package (docker-compose.yml + .env.template + README.txt) — multi-arch (amd64 + arm64) *(v0.8.0 candidate)*
- [x] Backup & Restore — Data Management tab in Settings with download backup, restore from file, active-run guard, and atomic DB replacement *(v0.7.0)*
- [x] Expose app via external nginx proxy with SSL termination — trust `X-Forwarded-*` headers (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`) *(v0.6.0)*
- [x] Azure CLI disclaimer on Discovery page when running in a container *(v0.6.0)*
- [x] Fix Discovery page to acquire a properly scoped Dataverse token for agent loading *(v0.6.0)*
- [x] Filter bar on Dashboard → Recent Runs (Suite Name / Agent / Date / Pass Rate) *(v0.6.0)*
- [x] Agent Leaderboard card on Dashboard (replaces static System Information) *(v0.6.0)*
- [x] Display version number (from `VERSION` file) on Help page *(v0.6.0)*
- [x] Runtime Environment card on Help page (container vs bare metal, auth mode, proxy headers) *(v0.6.0)*
