MaaJ — Copilot Studio Test Runner
Quick Start Guide
=====================================================

You received this file as part of the quickstart zip attached to a
GitHub Release. To always get the latest version, visit:
  https://github.com/holgerimbery/MaaJforMCS/releases/latest

Prerequisites
-------------
- Docker Desktop  https://www.docker.com/products/docker-desktop/
- An Azure OpenAI or Azure AI Foundry deployment

Steps
-----
1. Copy .env.template to .env
     Windows:   copy .env.template .env
     Mac/Linux: cp .env.template .env

2. Open .env in a text editor and fill in:
     JUDGE_ENDPOINT  — your Azure OpenAI endpoint URL
     JUDGE_API_KEY   — your API key
     JUDGE_MODEL     — deployment name (e.g. gpt-4o)

   Authentication is DISABLED by default (AUTHENTICATION_ENABLED=false).
   Anyone who can reach the app gets full Admin access.
   This is fine on a trusted internal network or behind a VPN.

   ⚠ If the app will be internet-accessible, set AUTHENTICATION_ENABLED=true
     and fill in the AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET
     values before starting.
   See https://github.com/holgerimbery/MaaJforMCS/wiki/Entra-ID-Setup

3. Start the application:
     docker compose up -d

4. Open your browser at:
     http://localhost:5062

5. The Setup Wizard guides you through creating your first agent and
   test suite.

Stopping
--------
  docker compose down

Data & Backups
--------------
All data is stored in a Docker named volume (maaj-data). It persists across
restarts and survives 'docker compose down' (but NOT 'docker compose down -v').
Use the built-in Backup & Restore in Settings → Data Management to export
a portable copy of your database.

Documentation
-------------
  https://github.com/holgerimbery/MaaJforMCS/wiki

Security note
-------------
Never expose this application directly to the internet without a
reverse proxy with TLS termination (nginx, Caddy, Traefik, etc.).
See https://github.com/holgerimbery/MaaJforMCS/wiki/Docker-Deployment
