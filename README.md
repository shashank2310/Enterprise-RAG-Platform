# Enterprise RAG Platform

Clean-architecture starter for an enterprise retrieval-augmented generation system using a .NET API, React, PostgreSQL, Azure OpenAI, and Azure AI Search.

## Current status

The API and React frontend are scaffolded, including MSAL sign-in and bearer-token acquisition when the public SPA settings are supplied. The recursive chunker and PostgreSQL schema are present. The protected query endpoint intentionally returns `503 Service Unavailable` until orchestration and data adapters are implemented, and the ready probe remains unavailable. Do not expose this starter to production workloads yet. See [the architecture and implementation blueprint](docs/architecture.md) for components, security requirements, index schema, diagrams, and the remaining delivery plan.

The code targets .NET 9 per the request. .NET 9 is out of support as of May 12, 2026; move to .NET 10 LTS before production launch.

## Local development

Prerequisites: .NET 9 SDK, Node.js 22, npm, and Docker Compose.

```powershell
Copy-Item .env.example .env
# Replace POSTGRES_PASSWORD with a unique local password in .env
docker compose up --build
```

- React UI: http://localhost:3000
- API liveness: http://localhost:5283/health/live
- API readiness: http://localhost:5283/health/ready (503 until adapters are wired)
- PostgreSQL: localhost:5432

For SPA sign-in, register a single-page application in Microsoft Entra ID with `http://localhost:3000` as a redirect URI. Expose a delegated API scope (for example, `access_as_user`) on the API registration and assign the user the API's `Rag.Query` app role. Set `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, and `VITE_ENTRA_API_SCOPE` in the root `.env` before building with Compose. For Vite development outside Docker, copy `frontend/.env.example` to `frontend/.env.local` and set the same values. These `VITE_*` settings are public client identifiers/scope names, not secrets; never put credentials or secrets in them. Register the frontend's deployed origin as a redirect URI for each environment.

The GitHub deploy workflow expects `VITE_API_BASE_URL`, `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, and `VITE_ENTRA_API_SCOPE` as environment-scoped GitHub **variables**, plus the Azure OIDC values described in the workflow. The API's Entra audience/tenant and CORS allowed origins must also be configured for that environment.

Build separately:

```powershell
dotnet build Enterprise-RAG-Platform.slnx --configuration Release
Set-Location frontend
npm ci
npm run build
```

The local database schema is applied once when the PostgreSQL data volume is initialized. To reapply it from scratch, stop the stack and remove only the named `postgres-data` volume after confirming the local data is disposable.

## Azure setup

No Azure resources or credentials are provisioned by this repository. Follow the architecture guide to create isolated environments and configure GitHub OIDC, managed identities, private network access, and Entra app roles. Never commit `.env`, secrets, tokens, or production connection strings.
Copy-Item .env.example .env