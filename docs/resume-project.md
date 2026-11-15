# Resume and portfolio wording

Use the wording that matches the role and your own contribution. Keep the prototype/scale qualification visible; do not describe the target Azure architecture as a live production deployment.

## Resume entry

**Enterprise RAG Platform | .NET 9, React, PostgreSQL, Ollama, Docker**

- Built a full-stack local RAG prototype with a React/TypeScript workspace and ASP.NET Core API for multi-document ingestion, retrieval, and evidence-cited question answering; added per-file upload progress and streamed answer/status updates.
- Implemented TXT, HTML, DOCX, and text-based PDF extraction, recursive overlapping chunking, Ollama embeddings, tenant-context-aware PostgreSQL persistence, and cosine-similarity top-K retrieval.
- Added evidence-conditioned Qwen generation with server-side mapping of model source labels to retrieved chunk IDs and a top-ranked source fallback when the model omits citation syntax.
- Added development-only local authentication, Entra/MSAL integration paths, API role policies, Docker Compose orchestration, readiness checks, and synthetic cross-format RAG validation documents.

## Short project summary

Designed and built a local-first enterprise RAG prototype that ingests common business-document formats, embeds and indexes content, retrieves tenant-scoped evidence, and generates answers with verifiable source citations. The codebase uses clean-architecture boundaries and leaves a documented path toward Azure OpenAI and Azure AI Search, while clearly separating the runnable local implementation from production roadmap items.

## 30-second demo narrative

> “This project demonstrates the end-to-end RAG lifecycle rather than just a chat prompt. I upload a policy or runbook, extract and recursively chunk its content, generate embeddings with a local Ollama model, and store chunks and vectors in PostgreSQL. At query time the API retrieves the nearest tenant-scoped chunks and passes only that evidence to the answer model. Citations are checked against the retrieved chunk IDs, and the system abstains if it cannot produce a valid citation. The current PostgreSQL cosine-search path is deliberately a small local prototype; Azure AI Search and production operations are documented as the next stage.”

## Technical discussion points

- **Why local models?** The Ollama adapter makes the demo runnable without an OpenAI API key and keeps model-provider concerns behind application interfaces.
- **Why PostgreSQL?** It provides durable document/chunk storage for the local demo and avoids requiring a separate vector service at small scale. The current embedding scan is not presented as a production search design.
- **Why citations by chunk ID?** The generator can only cite evidence supplied to the request, and the adapter filters returned markers to IDs actually present in retrieved context.
- **What did you test?** The synthetic corpus exercises exact facts, cross-section policy rules, HTML cleanup, OCR boundaries, and missing-answer abstention. It is a manual validation corpus, not a scored quality benchmark.
- **What is next?** Move to .NET 10 LTS; add scalable Azure AI Search indexing, background ingestion and lifecycle controls, security hardening, observability/cost tracking, calibrated confidence, and a versioned evaluation suite.

## Do not claim yet

Do not claim production Azure deployment, high availability, production-scale vector search, fully implemented hybrid/BM25/semantic ranking, semantic chunking, query rewriting, reranking, calibrated confidence, automated faithfulness/hallucination evaluation, or production-ready authorization/isolation. These are roadmap or architecture topics, not validated end-to-end capabilities of the current local demo.
