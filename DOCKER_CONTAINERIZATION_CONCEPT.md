# Docker Containerization Concept
## Copilot Studio Test Runner for Multi-Agent Coordination System

**Date:** February 18, 2026  
**Version:** 1.0  
**Status:** Concept Phase

---

## Executive Summary

This document outlines the containerization strategy for the Copilot Studio Test Runner application, enabling deployment in Docker environments. The solution supports both the WebUI (Blazor Server) and CLI components, with considerations for data persistence, configuration management, and production readiness.

---

## 1. Application Architecture Overview

### Current Application Components

| Component | Type | Framework | Purpose |
|-----------|------|-----------|---------|
| **WebUI** | Blazor Server | .NET 9.0 | Primary web interface for test management |
| **CLI** | Console App | .NET 9.0 | Command-line interface for automation |
| **Core** | Class Library | .NET 9.0 | Business logic and services |
| **Domain** | Class Library | .NET 9.0 | Domain entities and models |
| **Data** | Class Library | .NET 9.0 | EF Core data access |

### External Dependencies
- **Direct Line API**: Microsoft Bot Framework communication
- **Azure AI Foundry**: LLM-based test evaluation (Judge)
- **Azure OpenAI**: Question generation (optional)
- **SQLite**: Local database
- **File System**: Document uploads, logs, Lucene index

---

## 2. Containerization Approach

### 2.1 Multi-Container Strategy

We recommend a **modular approach** with separate containers for different concerns:

#### Primary Containers

1. **App Container** (`copilot-test-runner-web`)
   - Runs the WebUI Blazor application
   - Exposes HTTP/HTTPS endpoints
   - Implements health checks
   - Handles API and UI requests

2. **CLI Container** (`copilot-test-runner-cli`) *(Optional)*
   - Runs batch operations
   - Used for automation/CI-CD pipelines
   - Shares data volume with WebUI

#### Supporting Components

3. **Data Volume**
   - Persistent SQLite database
   - Document uploads storage
   - Lucene search index
   - Application logs

### 2.2 Single vs Multi-Container

| Approach | Pros | Cons | Recommendation |
|----------|------|------|----------------|
| **Single Container** | Simple deployment, easier development | Monolithic, harder to scale | ✅ Start here (MVP) |
| **Multi-Container** | Better separation, scalable | More complex orchestration | Future enhancement |

**Decision**: Start with single container for WebUI, optionally add CLI container.

---

## 3. Technical Architecture

### 3.1 Base Image Selection

```dockerfile
# Recommended base images
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime  # Production
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build      # Build stage
```

**Rationale**:
- ✅ Official Microsoft images
- ✅ Alpine variant for smaller size (~100MB vs ~200MB)
- ✅ Security updates maintained by Microsoft
- ✅ .NET 9.0 matched to application

### 3.2 Multi-Stage Build Strategy

```
Stage 1: Build (SDK image)
  ├─ Restore NuGet packages
  ├─ Build all projects
  ├─ Publish WebUI (self-contained or runtime-dependent)
  └─ Output: /app/publish

Stage 2: Runtime (ASP.NET runtime image)
  ├─ Copy published artifacts
  ├─ Set up non-root user
  ├─ Configure volumes
  └─ Expose ports
```

**Benefits**:
- Smaller final image (excludes SDK, source code)
- Faster deployment
- Enhanced security (no build tools in production)

### 3.3 Volume Management

#### Persistent Volumes Required

| Volume | Purpose | Path in Container | Size Estimate |
|--------|---------|-------------------|---------------|
| **database** | SQLite DB | `/app/data/db` | 10-500MB |
| **uploads** | Document storage | `/app/data/uploads` | Variable (user content) |
| **logs** | Application logs | `/app/logs` | 100MB-1GB |
| **index** | Lucene search index | `/app/data/index` | 50-200MB |

#### Volume Mount Strategy

```yaml
# Example docker-compose volume configuration
volumes:
  copilot-data:
    driver: local
  copilot-logs:
    driver: local
```

### 3.4 Network & Port Configuration

| Port | Protocol | Purpose | Expose? |
|------|----------|---------|---------|
| 5000 | HTTP | Development | Optional |
| 8080 | HTTP | Production | ✅ Yes |
| 8443 | HTTPS | Production (SSL) | ✅ Yes |

**Security Note**: Never expose SQLite port (file-based), use reverse proxy for HTTPS termination.

---

## 4. Configuration Management

### 4.1 Environment Variables Strategy

The application currently uses placeholders like `${DL_SECRET}`. Docker approach:

```bash
# Environment variables for runtime configuration
DIRECTLINE__SECRET=your-secret-here
DIRECTLINE__BOTID=your-bot-id
JUDGE__ENDPOINT=https://your-ai-foundry-endpoint
JUDGE__APIKEY=your-api-key
JUDGE__MODEL=gpt-4o-mini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

### 4.2 Configuration Sources Priority

1. **Environment Variables** (Highest priority - Production)
2. **Docker Secrets** (Kubernetes/Swarm)
3. **appsettings.json** (Defaults)
4. **appsettings.Production.json** (Production overrides)

### 4.3 Secrets Management

**Options**:

| Method | Use Case | Security Level |
|--------|----------|----------------|
| `.env` file | Development only | ⚠️ Low |
| Docker Secrets | Swarm/Compose | ✅ High |
| Azure Key Vault | Azure deployment | ✅ Very High |
| Kubernetes Secrets | K8s deployment | ✅ High |

**Recommendation**: Docker Secrets for secrets, environment variables for non-sensitive config.

---

## 5. Data Persistence & Backup

### 5.1 SQLite in Docker Considerations

**Challenges**:
- ❌ SQLite performs poorly with NFS/some volume drivers
- ❌ Locking issues in distributed scenarios
- ❌ No built-in replication

**Solutions**:
1. **Local volumes** (recommended for single-instance)
2. **Volume with proper driver** (use `local` driver, not NFS)
3. **Future migration path to PostgreSQL** for production scale

### 5.2 Backup Strategy

```bash
# Example backup approach
docker exec copilot-web sqlite3 /app/data/db/app.db ".backup '/app/data/backups/backup-$(date +%Y%m%d).db'"
```

**Automated Options**:
- Cron job in separate container
- Host-level backup of volume
- Cloud storage sync (Azure Blob, AWS S3)

---

## 6. Health Checks & Monitoring

### 6.1 Docker Health Check

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

**Application Support**: 
- ✅ Application already has `/health` endpoint via `AddHealthChecks()`

### 6.2 Monitoring Endpoints

| Endpoint | Purpose | Status |
|----------|---------|--------|
| `/health` | Basic health | ✅ Implemented |
| `/api/metrics/summary` | Application metrics | ✅ Implemented |
| `/metrics` (Prometheus) | Container metrics | 🔄 Future enhancement |

---

## 7. Deployment Scenarios

### 7.1 Development (Local)

**Goal**: Quick iteration, debugging capabilities

```bash
docker-compose up
# - Source code mounted as volume (hot reload)
# - Exposed ports for debugging
# - Verbose logging enabled
```

### 7.2 Production (Single Server)

**Goal**: Stability, security, performance

```bash
docker run -d \
  --name copilot-test-runner \
  --restart unless-stopped \
  -v copilot-data:/app/data \
  -v copilot-logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --env-file /secure/secrets.env \
  -p 8080:8080 \
  copilot-test-runner:latest
```

### 7.3 Production (Orchestrated - Kubernetes/Swarm)

**Goal**: High availability, scalability

- Multiple replicas (with shared storage considerations)
- Load balancer
- Auto-scaling
- Secret management
- Health-based restarts

**Challenge**: SQLite limits horizontal scaling. Consider PostgreSQL migration.

---

## 8. Security Considerations

### 8.1 Container Security Best Practices

✅ **Implemented in Concept**:
- Run as non-root user
- Minimal base image (Alpine)
- No unnecessary packages
- Multi-stage builds (no build tools in production)
- Read-only root filesystem (where possible)

### 8.2 Application Security

- ✅ HTTPS support (configure certificates)
- ✅ Secrets via environment variables (not in image)
- ✅ API authentication (review current implementation)
- ⚠️ Rate limiting for public APIs
- ⚠️ CORS configuration for production

### 8.3 Network Security

```yaml
# Example network isolation
networks:
  frontend:
    external: true  # Exposed to internet (via reverse proxy)
  backend:
    internal: true  # Internal only, no external access
```

---

## 9. Performance Optimization

### 9.1 Build Performance

- **Layer caching**: Order Dockerfile commands for optimal caching
- **Multi-stage builds**: Parallel stage execution
- **.dockerignore**: Exclude unnecessary files from build context

### 9.2 Runtime Performance

- **Alpine base**: Smaller image, faster pulls
- **Runtime-dependent**: Smaller than self-contained (relies on runtime in base image)
- **Volume mounts**: Local driver for SQLite performance

### 9.3 Image Size Optimization

**Target Sizes**:
- Development image: ~300-400MB (with SDK)
- Production image: ~100-150MB (runtime only)

---

## 10. Migration Path & Rollout Plan

### Phase 1: MVP (Weeks 1-2)
- ✅ Create Dockerfile for WebUI
- ✅ Create docker-compose.yml for local development
- ✅ Test basic functionality
- ✅ Document setup process

### Phase 2: Production-Ready (Weeks 3-4)
- 🔄 Add health checks
- 🔄 Implement secrets management
- 🔄 Create CLI container (optional)
- 🔄 Performance testing & optimization
- 🔄 Production deployment guide

### Phase 3: Enhancement (Future)
- 💡 PostgreSQL migration option
- 💡 Kubernetes manifests
- 💡 Prometheus metrics export
- 💡 Auto-scaling configuration
- 💡 CI/CD pipeline integration

---

## 11. Testing Strategy

### 11.1 Container Testing

```bash
# Build test
docker build -t copilot-test-runner:test .

# Smoke test
docker run --rm -p 8080:8080 copilot-test-runner:test

# Health check test
curl http://localhost:8080/health

# Integration test
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

### 11.2 Validation Checklist

- [ ] Container starts successfully
- [ ] Health endpoint responds
- [ ] Database migrations run automatically
- [ ] API endpoints accessible
- [ ] UI loads correctly
- [ ] File uploads work
- [ ] Direct Line connection succeeds
- [ ] Test execution completes
- [ ] Logs written to volume
- [ ] Container restarts gracefully

---

## 12. Documentation Requirements

### 12.1 User Documentation
- Docker installation guide
- Quick start guide
- Environment variable reference
- Troubleshooting common issues

### 12.2 Developer Documentation
- Build instructions
- Local development setup
- Debugging in containers
- Contributing guidelines

### 12.3 Operations Documentation
- Deployment procedures
- Backup and restore
- Monitoring and alerting
- Scaling guidelines

---

## 13. Alternative Approaches Considered

### 13.1 Database Alternatives

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| **Keep SQLite** | Simple, no external dependencies | Limited scalability | ✅ Phase 1 |
| **PostgreSQL** | Production-grade, scalable | Additional container, complexity | 🔄 Phase 3 option |
| **SQL Server in Docker** | Enterprise features | Large image, licensing | ❌ Rejected |

### 13.2 File Storage Alternatives

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| **Local volumes** | Simple, fast | Limited to single host | ✅ MVP |
| **Azure Blob Storage** | Scalable, durable | Requires Azure, latency | 🔄 Future option |
| **NFS/CIFS** | Shared across hosts | Complex setup, performance | ⚠️ Not recommended |

---

## 14. Success Criteria

### Must Have (MVP)
- ✅ Application runs in container
- ✅ Data persists across container restarts
- ✅ Configuration via environment variables
- ✅ Logs accessible outside container
- ✅ Health checks functional

### Should Have (Production)
- 🔄 Automated builds (CI/CD)
- 🔄 Image published to registry
- 🔄 Secrets management implemented
- 🔄 Production deployment guide complete
- 🔄 Monitoring configured

### Nice to Have (Future)
- 💡 Kubernetes support
- 💡 Horizontal scaling solution
- 💡 Database migration to PostgreSQL
- 💡 Automated backups
- 💡 Blue-green deployment support

---

## 15. Next Steps

### Immediate Actions (Ready to Implement)

1. **Create Dockerfile**
   - Multi-stage build for WebUI
   - Alpine-based runtime
   - Health checks configured

2. **Create docker-compose.yml**
   - Service definition
   - Volume configuration
   - Environment variable template

3. **Create .dockerignore**
   - Exclude unnecessary files
   - Optimize build context

4. **Create .env.template**
   - Document all required variables
   - Provide example values

5. **Update README.md**
   - Add Docker deployment section
   - Quick start guide

### Approval Required Before Proceeding

- [ ] Approve containerization approach
- [ ] Approve security measures
- [ ] Approve volume/persistence strategy
- [ ] Approve configuration management approach

---

## 16. Questions & Decisions Needed

1. **Registry**: Where should Docker images be published?
   - Docker Hub (public/private)
   - Azure Container Registry
   - GitHub Container Registry
   - Self-hosted registry

2. **Versioning**: What versioning scheme for images?
   - Semantic versioning (1.0.0, 1.0.1, etc.)
   - Git commit SHA
   - Date-based (2026.02.18)
   - Combination (1.0.0-20260218-a1b2c3d)

3. **CI/CD**: Which platform for automated builds?
   - GitHub Actions
   - Azure DevOps
   - GitLab CI
   - Jenkins

4. **Target Environment**: Primary deployment scenario?
   - Single server (Docker Compose)
   - Kubernetes cluster
   - Azure Container Instances
   - AWS ECS/Fargate

---

## Appendix A: Technology Stack

- **Runtime**: .NET 9.0
- **Framework**: ASP.NET Core (Blazor Server)
- **Database**: SQLite (EF Core)
- **Container Platform**: Docker 24.0+
- **Orchestration**: Docker Compose (MVP), Kubernetes (future)
- **Base Image**: mcr.microsoft.com/dotnet/aspnet:9.0-alpine

## Appendix B: Estimated Effort

| Task | Estimated Hours |
|------|-----------------|
| Dockerfile creation | 4-6 hours |
| docker-compose setup | 2-3 hours |
| Testing & validation | 4-8 hours |
| Documentation | 3-4 hours |
| Production hardening | 8-12 hours |
| **Total (MVP)** | **21-33 hours** |

## Appendix C: Glossary

- **Multi-stage build**: Docker technique to create smaller production images
- **Health check**: Automated test to verify container is functioning
- **Volume**: Persistent storage mechanism in Docker
- **Alpine**: Minimal Linux distribution (~5MB base size)
- **Direct Line**: Microsoft Bot Framework API for bot communication

---

**End of Concept Document**

Ready for review and approval to proceed with implementation.
