# Resume and portfolio wording

Keep the prototype and scale qualifications visible. Describe only work you personally performed, and do not present the Azure target design as a live production deployment.

## Resume entry

**Enterprise RAG Platform | .NET 10, React, PostgreSQL, Ollama, Docker**

- Built a local-first RAG prototype with a React/TypeScript workspace and ASP.NET Core API for multi-document ingestion, retrieval, and evidence-cited question answering, including per-file upload results and streamed status/answer previews.
- Implemented TXT, HTML, DOCX, and selectable-text PDF extraction, recursive overlapping chunks, Ollama embeddings, tenant-scoped PostgreSQL persistence, and in-memory cosine-similarity top-K retrieval for a small development corpus.
- Added evidence-conditioned generation that maps model citation labels only to retrieved chunks; the Ollama path attaches its highest-ranked retrieved chunk when the model omits a citation. Confidence is currently a placeholder, not a quality score.
- Added Development-only local authentication, Entra/MSAL and API role-policy integration paths, Docker Compose setup, readiness checks, and a fictional multi-format validation corpus.

## Short project summary

Designed and built a local RAG demonstration that takes common business documents through extraction, chunking, embedding, tenant-scoped retrieval, and grounded answer generation. The codebase separates API, application, domain, and infrastructure concerns and documents a path toward Azure OpenAI, Azure AI Search, Blob Storage, and queue-backed ingestion. Azure adapters are integration building blocks; the repository does not provision or deliver a complete Azure deployment.

## 30-second demo narrative

> “This project demonstrates the RAG lifecycle, not just a chat prompt. I upload a policy or runbook, extract and recursively chunk its text, create embeddings with Ollama, and store chunks and vectors in PostgreSQL. For a question, the API retrieves the closest chunks for the current tenant and sends only those passages to the answer model. The final response maps citations to retrieved evidence, while confidence remains uncalibrated. The local PostgreSQL cosine scan is intentionally a small-corpus demo; the Azure Search indexing lifecycle and production operations are next steps, not features I claim are already deployed.”

## Technical discussion points

- **Why local models?** Ollama makes the demonstration usable without an OpenAI API key and keeps provider implementations behind application interfaces.
- **Why PostgreSQL?** It gives the local demo one durable store for metadata, chunks, and JSONB vectors. Candidate vectors are scored in API memory, so it is not a production vector-search design.
- **What do citations guarantee?** They resolve to retrieved chunk IDs, not that every generated statement is correct. The local Ollama path falls back to the top-ranked retrieved chunk if the model provides no valid label; confidence is always `0` today.
- **What was validated?** Fictional sample documents exercise exact facts, composed rules, HTML cleanup, OCR boundaries, and an intentionally unanswerable question. This is manual validation, not a scored benchmark.
- **What is next?** Complete identity and tenant-isolation tests; add Search indexing/upsert/delete; make ingestion durable with Blob Storage and a queue-backed worker; then add privacy-safe observability, cost controls, and a versioned evaluation suite.

## Do not claim yet

Do not claim a production Azure deployment, high availability, scalable vector search, complete Azure AI Search indexing, group-level ACL enforcement in the local retriever, asynchronous/retryable ingestion, malware scanning, semantic chunking, query rewriting, reranking, calibrated confidence, automated faithfulness evaluation, or production-ready authorization/isolation. These are roadmap topics, not validated end-to-end local-demo features.
