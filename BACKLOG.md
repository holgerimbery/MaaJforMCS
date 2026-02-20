# Backlog

## Test Quality

- [ ] **Conversation flow testing** — multi-turn test cases that verify the agent handles context across several exchanges, not just single Q&A
- [ ] **Regression detection** — flag when a test that previously passed now fails, with a diff of the judge rationale
- [ ] **Test case import from CSV/Excel** — bulk upload questions instead of entering them one by one
- [ ] **Question generation wizard** — guided wizard that generates test cases from an uploaded knowledge base document

## Evaluation & Insights

- [ ] **Side-by-side agent comparison** — run the same suite against two agents and show a diff of pass rates, latency and scores
- [ ] **Confidence scoring trends** — track judge score trends per individual test case over time to spot degrading answers
- [ ] **Latency percentile chart (P50/P95/P99)** — visual over time, not just the current average
- [ ] **Topic/tag grouping** — tag test cases by topic and show pass rates per topic to identify weak subject areas

## Operations

- [ ] **Scheduled runs** — cron-style scheduling so suites run automatically (e.g. nightly) without manual trigger
- [ ] **Webhook / Teams notification** — post a summary card to a Teams channel when a run completes or a regression is detected
- [ ] **Run history pruning** — configurable retention policy to keep the SQLite database from growing unbounded
- [ ] **Export run report to PDF/Excel** — one-click download of a formatted test report for sharing with stakeholders

## Security & Multi-tenancy

- [ ] **Role-based test suite ownership** — users can only edit/delete suites they created; admins see everything
- [ ] **Audit log page** — surface the existing `AuditLog` entity in the UI

## Developer Experience

- [ ] **REST API key auth** — allow CI pipelines to trigger runs via the existing API without browser login
- [ ] **OpenTelemetry traces** — instrument test execution so each run has a trace ID linkable to Azure Monitor

## Completed

- [x] Expose app via external nginx proxy with SSL termination — trust `X-Forwarded-*` headers (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`)
- [x] Azure CLI disclaimer on Discovery page when running in a container
- [x] Fix Discovery page to acquire a properly scoped Dataverse token for agent loading
- [x] Filter bar on Dashboard → Recent Runs (Suite Name / Agent / Date / Pass Rate)
- [x] Agent Leaderboard card on Dashboard (replaces static System Information)
- [x] Display version number (from `VERSION` file) on Help page
- [x] Runtime Environment card on Help page (container vs bare metal, auth mode, proxy headers)
