# Enterprise RAG Platform

Clean-architecture starter for an enterprise retrieval-augmented generation system using a .NET API, React, PostgreSQL, Azure OpenAI, and Azure AI Search.

## Current status

The API and React frontend include MSAL sign-in, a protected document-upload endpoint, Ollama chat and embedding adapters, and a local PostgreSQL vector-similarity prototype. Configure Entra app roles and download the Ollama chat/embedding models before indexing or querying. The local PostgreSQL retriever scans tenant-scoped embeddings in application memory and is for small development corpora only; production should use a scalable vector index and complete the remaining security, lifecycle, and evaluation work. See [the architecture and implementation blueprint](docs/architecture.md) for current limitations and the delivery plan.

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
- API readiness: http://localhost:5283/health/ready (checks PostgreSQL and the configured Ollama models)
- PostgreSQL: localhost:5432

Before using Ollama, download both required models in a separate PowerShell window while Ollama is running:

```powershell
ollama pull qwen3:8b
ollama pull nomic-embed-text
```

Compose connects the API container to Ollama on the Windows host through `host.docker.internal:11434`. On Windows, if the readiness endpoint says Ollama is unreachable, set the user environment variable `OLLAMA_HOST=0.0.0.0:11434`, quit and restart Ollama, then check `http://localhost:11434/api/tags`. Binding to all interfaces can expose the unauthenticated Ollama API to your network; only do this on a trusted private network and restrict access with Windows Firewall. If you chose a different chat model, set `OLLAMA_CHAT_MODEL` in the root `.env`; set `OLLAMA_EMBEDDING_MODEL` if you use another Ollama embedding model. Rebuild/restart Compose after changing model settings. Keep the same embedding model for all indexed documents; changing it requires re-indexing the documents.

For SPA sign-in, register a single-page application in Microsoft Entra ID with `http://localhost:3000` as a redirect URI. Expose a delegated API scope (for example, `access_as_user`) on the API registration. Define and assign the signed-in user the API's `Rag.Query` and `Rag.Ingest` app roles (or `Rag.Admin`). Set `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, and `VITE_ENTRA_API_SCOPE` in the root `.env` before building with Compose. Set `API_ENTRA_TENANT_ID` and `API_ENTRA_CLIENT_ID` in the same file to the API app registration's tenant and client IDs. For Vite development outside Docker, copy `frontend/.env.example` to `frontend/.env.local` and set the SPA values there. These client IDs and scopes are public identifiers, not secrets; never put credentials or secrets in `VITE_*` settings. Register the frontend's deployed origin as a redirect URI for each environment.

The UI has a document upload form. Upload a PDF, DOCX, TXT, or HTML file; the API extracts text, recursively chunks it, generates Ollama embeddings, and stores chunks/vectors in PostgreSQL. PDF extraction still requires Azure AI Document Intelligence; TXT, DOCX, and HTML work locally. Ask questions after indexing; the API embeds each question, compares it with chunks belonging to the caller's Entra tenant, and asks Qwen to answer from the retrieved evidence with validated citations. Confidence remains 0 until a calibrated scoring component is implemented. Upload and query roles are enforced by the API (`Rag.Ingest`/`Rag.Admin` and `Rag.Query`/`Rag.Admin`).

To try a small Hugging Face corpus, the Amnesty QA dataset has an English v3 evaluation split with 20 question/context examples and is marked Apache-2.0. Export only its retrieved evidence contexts (not question/reference answers) to a TXT file, then upload that file through the UI:

```powershell
py -m pip install datasets
py scripts/export_hf_amnesty_contexts.py
```

The script writes `samples/amnesty_qa_contexts.txt`; select that file in the upload form. Check the dataset card and license before reusing data outside local evaluation. Source: [Hugging Face Amnesty QA dataset](https://huggingface.co/datasets/explodinggradients/amnesty_qa).

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