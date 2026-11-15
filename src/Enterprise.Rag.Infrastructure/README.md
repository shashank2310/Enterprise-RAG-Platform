# Infrastructure adapters

This project contains Ollama chat and embedding adapters, Azure OpenAI and Azure AI Search adapters, document text extraction, and a local PostgreSQL chunk/embedding index. The Ollama/PostgreSQL path is intended for small local development corpora: embeddings are stored as JSONB and compared in application memory within the authenticated tenant's RLS-scoped transaction. It is not a scalable production vector index.

The local upload path stores extracted chunks and embeddings, not the original file. TXT, DOCX, HTML, and text-based PDF extraction are local; scanned/image-only PDFs need Azure AI Document Intelligence OCR. Production still needs durable source-file storage, malware/signature validation, background ingestion, scalable vector search, group ACL enforcement, and the remaining evaluation/observability work. Keep secrets out of configuration and browser builds; production workloads should use managed identity and a secrets manager.
