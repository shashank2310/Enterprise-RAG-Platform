CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS documents (
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	tenant_id text NOT NULL,
	file_name text NOT NULL,
	content_type text NOT NULL,
	sha256 char(64) NOT NULL,
	blob_uri text NOT NULL,
	status text NOT NULL CHECK (status IN ('Pending', 'Processing', 'Indexed', 'Failed', 'Deleted')),
	created_by text NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	UNIQUE (tenant_id, sha256)
);

CREATE TABLE IF NOT EXISTS document_chunks (
	id text PRIMARY KEY,
	tenant_id text NOT NULL,
	document_id uuid NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
	ordinal integer NOT NULL CHECK (ordinal >= 0),
	page_number integer,
	content text NOT NULL,
	metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
	created_at timestamptz NOT NULL DEFAULT now(),
	UNIQUE (tenant_id, document_id, ordinal)
);

CREATE INDEX IF NOT EXISTS ix_document_chunks_tenant_document ON document_chunks (tenant_id, document_id);
CREATE INDEX IF NOT EXISTS ix_documents_tenant_status ON documents (tenant_id, status);
CREATE INDEX IF NOT EXISTS ix_document_chunks_metadata ON document_chunks USING gin (metadata);

CREATE TABLE IF NOT EXISTS chat_sessions (
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	tenant_id text NOT NULL,
	subject_id text NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	last_activity_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS chat_messages (
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	tenant_id text NOT NULL,
	session_id uuid NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
	role text NOT NULL CHECK (role IN ('user', 'assistant', 'system')),
	content text NOT NULL,
	citations jsonb NOT NULL DEFAULT '[]'::jsonb,
	confidence double precision CHECK (confidence BETWEEN 0 AND 1),
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS evaluation_runs (
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	tenant_id text NOT NULL,
	query_id uuid,
	faithfulness double precision CHECK (faithfulness BETWEEN 0 AND 1),
	answer_relevancy double precision CHECK (answer_relevancy BETWEEN 0 AND 1),
	context_precision double precision CHECK (context_precision BETWEEN 0 AND 1),
	context_recall double precision CHECK (context_recall BETWEEN 0 AND 1),
	hallucination_detected boolean NOT NULL DEFAULT false,
	evaluator_version text NOT NULL,
	details jsonb NOT NULL DEFAULT '{}'::jsonb,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS usage_events (
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	tenant_id text NOT NULL,
	trace_id text,
	model_deployment text NOT NULL,
	operation text NOT NULL,
	input_tokens integer NOT NULL DEFAULT 0 CHECK (input_tokens >= 0),
	output_tokens integer NOT NULL DEFAULT 0 CHECK (output_tokens >= 0),
	estimated_cost numeric(18, 8),
	created_at timestamptz NOT NULL DEFAULT now()
);

-- Enable and FORCE RLS for every tenant-owned table. Set app.tenant_id with SET LOCAL
-- inside each transaction after validating the Entra token; use a non-owner runtime role.
DO $$
DECLARE
	table_name text;
BEGIN
	FOREACH table_name IN ARRAY ARRAY['documents', 'document_chunks', 'chat_sessions', 'chat_messages', 'evaluation_runs', 'usage_events']
	LOOP
		EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', table_name);
		EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', table_name);
		EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I', table_name);
		EXECUTE format(
			'CREATE POLICY tenant_isolation ON %I USING (tenant_id = current_setting(''app.tenant_id'', true)) WITH CHECK (tenant_id = current_setting(''app.tenant_id'', true))',
			table_name);
	END LOOP;
END $$;
