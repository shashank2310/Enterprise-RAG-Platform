# Enterprise RAG Platform

A local-first enterprise Retrieval-Augmented Generation (RAG) prototype built with .NET 9, React, PostgreSQL, and Ollama, with Azure OpenAI and Azure AI Search integration paths.

## Current status

The runnable local path supports sequential multi-document upload (up to five files per selection), TXT/HTML/DOCX/text-PDF extraction, recursive chunking, Ollama embeddings and chat, tenant-context-aware PostgreSQL persistence, cosine-similarity retrieval, live retrieval status, streamed answer tokens, and evidence citations. The local PostgreSQL vector scan is intended for small development corpora, not production-scale search. The Azure architecture is not a provisioned deployment; production hardening, scalable indexing, and formal evaluation remain roadmap work. See the [project showcase](docs/project-showcase.md), [resume and demo notes](docs/resume-project.md), and [architecture blueprint](docs/architecture.md).

The code targets .NET 9 per the request. .NET 9 is out of support as of May 12, 2026; move to .NET 10 LTS before production launch.

## GitHub showcase

- [12-slide project presentation](docs/enterprise-rag-showcase.pptx)
- [Implementation, local architecture, and demo walkthrough](docs/project-showcase.md)
- [Resume-ready project summary and interview narrative](docs/resume-project.md)
- [Synthetic RAG validation corpus](samples/rag-validation/README.md)

Regenerate the PowerPoint after installing Python's `python-pptx` package:

```powershell
py -m pip install python-pptx
py scripts/generate_showcase_deck.py
```

## Local development

Prerequisites: .NET 9 SDK, Node.js 22, npm, Docker Compose, and Ollama installed on the host. The smaller Qwen model is a practical starting point for local hardware.

1. Start Ollama on the host and download the models in a separate PowerShell window:

   ```powershell
   ollama pull qwen3:1.7b
   ollama pull nomic-embed-text
   ```

2. From the repository root, create `.env`, set a unique local PostgreSQL password, and start the app:

   ```powershell
   Copy-Item .env.example .env
   # Replace POSTGRES_PASSWORD; keep LOCAL_DEMO_AUTH=true for local testing.
   docker compose up --build
   ```

- React UI: http://localhost:3000
- API liveness: http://localhost:5283/health/live
- API readiness: http://localhost:5283/health/ready (checks PostgreSQL and the configured Ollama models)
- PostgreSQL: internal to Docker Compose (`postgres:5432`; no host port is published by default)

Set `OLLAMA_CHAT_MODEL` in `.env` if you choose another model. Keep the same embedding model for all indexed documents; changing it requires re-indexing.

Compose connects the API container to Ollama on the Windows host through `host.docker.internal:11434`. On Windows, if the readiness endpoint says Ollama is unreachable, set the user environment variable `OLLAMA_HOST=0.0.0.0:11434`, quit and restart Ollama, then check `http://localhost:11434/api/tags`. Binding to all interfaces can expose the unauthenticated Ollama API to your network; only do this on a trusted private network and restrict access with Windows Firewall. If you chose a different chat model, set `OLLAMA_CHAT_MODEL` in the root `.env`; set `OLLAMA_EMBEDDING_MODEL` if you use another Ollama embedding model. Rebuild/restart Compose after changing model settings. Keep the same embedding model for all indexed documents; changing it requires re-indexing the documents.

With the default `LOCAL_DEMO_AUTH=true`, no Entra registration or sign-in is needed: the local API supplies a fixed demo user with query and ingest roles, and the UI indicates `LOCAL DEMO`. The demo API and UI ports bind to `127.0.0.1`; PostgreSQL is internal to the Compose network and has no host port published by default. Do not expose the demo mode outside local Development. To use real Entra authentication instead, set `LOCAL_DEMO_AUTH=false` and configure the SPA/API app registrations, delegated API scope, `Rag.Query`/`Rag.Ingest` app roles, and the `VITE_*` and `API_ENTRA_*` values below. Register `http://localhost:3000` as an SPA redirect URI. Client IDs and scopes are public identifiers, not secrets; never put credentials in `VITE_*` settings.

The UI lets you select up to five documents (20 MB each) and indexes them sequentially, showing per-file progress and errors. The API extracts text, recursively chunks it, generates Ollama embeddings, and stores chunks/vectors in PostgreSQL. Text-based PDFs, DOCX, TXT, and HTML are processed locally; scanned/image-only PDFs need Azure AI Document Intelligence for OCR. Ask questions after indexing; live status events report retrieval and generation stages, then answer tokens stream into the UI. Model source labels are mapped only to retrieved chunks; if Qwen omits a label, the API attaches the highest-ranked retrieved chunk so the answer is not discarded. Confidence remains 0 until calibrated scoring is implemented. Local demo mode grants query/ingest roles to its fixed demo identity; Entra mode enforces `Rag.Ingest`/`Rag.Admin` and `Rag.Query`/`Rag.Admin` roles.

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

The repository's runnable helper scripts are `db/schema.sql` (applied automatically only when PostgreSQL initializes a new volume; manually run with `Get-Content .\db\schema.sql -Raw | docker compose exec -T postgres psql -U rag_app -d enterprise_rag`) and `scripts/export_hf_amnesty_contexts.py` (run with `py scripts\export_hf_amnesty_contexts.py` after installing the Python `datasets` package). The API also applies the local embedding-column schema update at startup. The database schema is designed to be re-applied, but the PostgreSQL RLS statements require the runtime tenant setting for application queries. Avoid deleting the `postgres-data` volume unless you intentionally want to erase the local database.

## Azure setup

No Azure resources or credentials are provisioned by this repository. Follow the architecture guide to create isolated environments and configure GitHub OIDC, managed identities, private network access, and Entra app roles. Never commit `.env`, secrets, tokens, or production connection strings.
Copy-Item .env.example .env