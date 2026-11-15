import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { MsalProvider, useMsal } from '@azure/msal-react';
import { useState, type FormEvent } from 'react';
import { apiScope, authConfigured, localDemoAuth, msalInstance } from './authConfig';

type Citation = { documentId: string; chunkId: string; fileName: string; pageNumber?: string };
type Answer = { answer: string; confidence: number; citations: Citation[] };
type IndexedDocument = { documentId: string; chunkCount: number; fileName: string };
type UploadOutcome = { fileName: string; message: string; success: boolean };
type QueryStreamEvent =
  | { type: 'status'; message: string }
  | { type: 'token'; text: string }
  | { type: 'complete'; answer: Answer }
  | { type: 'error'; message: string };
type WorkspaceProps = { getAccessToken?: () => Promise<string>; accountName?: string; onSignOut?: () => Promise<void>; localDemoAuth?: boolean };

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5283';

export default function App() {
  if (!authConfigured || !msalInstance) {
    return <Workspace localDemoAuth={localDemoAuth} />;
  }

  return (
    <MsalProvider instance={msalInstance}>
      <AuthenticatedWorkspace />
    </MsalProvider>
  );
}

function AuthenticatedWorkspace() {
  const { instance, accounts } = useMsal();
  const account = instance.getActiveAccount() ?? accounts[0];

  async function getAccessToken(): Promise<string> {
    if (!account) {
      const loginResult = await instance.loginPopup({ scopes: [apiScope] });
      if (!loginResult.account || !loginResult.accessToken) {
        throw new Error('Entra ID sign-in did not return an API access token.');
      }
      instance.setActiveAccount(loginResult.account);
      return loginResult.accessToken;
    }

    const request = { scopes: [apiScope], account };
    try {
      return (await instance.acquireTokenSilent(request)).accessToken;
    } catch (error) {
      if (!(error instanceof InteractionRequiredAuthError)) {
        throw error;
      }
      return (await instance.acquireTokenPopup(request)).accessToken;
    }
  }

  async function signOut(): Promise<void> {
    await instance.logoutPopup(account ? { account } : {});
  }

  return <Workspace getAccessToken={getAccessToken} accountName={account?.username} onSignOut={signOut} />;
}

function Workspace({ getAccessToken, accountName, onSignOut, localDemoAuth: isLocalDemo = false }: WorkspaceProps) {
  const [question, setQuestion] = useState('');
  const [answer, setAnswer] = useState<Answer | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState('');
  const [uploadResults, setUploadResults] = useState<UploadOutcome[]>([]);
  const [workingStatus, setWorkingStatus] = useState('');
  const [streamedAnswer, setStreamedAnswer] = useState('');

  async function uploadDocument(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setUploadProgress('');
    setUploadResults([]);
    if (selectedFiles.length === 0 || (!getAccessToken && !isLocalDemo)) {
      return;
    }
    if (selectedFiles.length > 5) {
      setUploadProgress('Select no more than five documents per upload batch.');
      return;
    }

    setUploading(true);
    const results: UploadOutcome[] = [];
    try {
      const accessToken = getAccessToken ? await getAccessToken() : undefined;
      for (const [index, file] of selectedFiles.entries()) {
        setUploadProgress(`Indexing document ${index + 1} of ${selectedFiles.length}: ${file.name}`);
        const formData = new FormData();
        formData.append('file', file);
      const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined;
            try {
              const response = await fetch(`${apiBaseUrl}/api/documents/upload`, {
                method: 'POST',
                headers,
                body: formData,
              });
              if (!response.ok) {
                const body = await response.json().catch(() => undefined);
                throw new Error(body?.detail ?? body?.title ?? `Upload failed (${response.status}).`);
              }
              const indexed = (await response.json()) as IndexedDocument;
              results.push({ fileName: indexed.fileName, success: true, message: `Indexed into ${indexed.chunkCount} chunks.` });
            } catch (reason) {
              results.push({
                fileName: file.name,
                success: false,
                message: reason instanceof Error ? reason.message : 'This document could not be indexed.',
              });
            }
            setUploadResults([...results]);
          }

          const indexedCount = results.filter((result) => result.success).length;
          setUploadProgress(`Finished: ${indexedCount} of ${results.length} documents indexed.`);
          setSelectedFiles([]);
          const input = document.getElementById('document-file') as HTMLInputElement | null;
          if (input) input.value = '';
        } catch (reason) {
          setUploadProgress(reason instanceof Error ? reason.message : 'Document indexing could not be started.');
        } finally {
          setUploading(false);
        }
      }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setAnswer(null);
    setStreamedAnswer('');
    setWorkingStatus('Preparing your request…');
    setBusy(true);

    try {
      if (!getAccessToken && !isLocalDemo) {
        throw new Error('Configure Entra ID or enable the Development-only local demo mode.');
      }
      const accessToken = getAccessToken ? await getAccessToken() : undefined;
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (accessToken) headers.Authorization = `Bearer ${accessToken}`;
      const response = await fetch(`${apiBaseUrl}/api/rag/query/stream`, {
        method: 'POST',
        headers: { ...headers, Accept: 'application/x-ndjson' },
        body: JSON.stringify({ question }),
      });
      if (!response.ok) {
        const body = await response.json().catch(() => undefined);
        throw new Error(body?.detail ?? body?.title ?? `Request failed (${response.status}).`);
      }
      if (!response.body) {
        throw new Error('The server did not provide a response stream.');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      let partialAnswer = '';
      let completed = false;
      const handleEvent = (line: string) => {
        if (!line.trim()) return;
        const event = JSON.parse(line) as QueryStreamEvent;
        if (event.type === 'status') {
          setWorkingStatus(event.message);
        } else if (event.type === 'token') {
          partialAnswer += event.text;
          setStreamedAnswer(partialAnswer);
          setWorkingStatus('Writing an evidence-based answer…');
        } else if (event.type === 'complete') {
          setAnswer(event.answer);
          setStreamedAnswer('');
          setWorkingStatus('');
          completed = true;
        } else if (event.type === 'error') {
          throw new Error(event.message);
        }
      };

      while (true) {
        const { value, done } = await reader.read();
        buffer += decoder.decode(value, { stream: !done });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';
        for (const line of lines) handleEvent(line);
        if (done) break;
      }
      if (buffer.trim()) handleEvent(buffer);
      if (!completed) throw new Error('The answer stream ended before a complete response arrived.');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The request could not be completed.');
      setStreamedAnswer('');
      setWorkingStatus('');
    } finally {
      setBusy(false);
    }
  }

  async function signOut() {
    try {
      await onSignOut?.();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Sign out could not be completed.');
    }
  }

  return (
    <main className="shell">
      <header className="topbar">
        <div className="brand-mark">K</div>
        <div><strong>Knowledge Hub</strong><span>Enterprise AI workspace</span></div>
        <span className="environment">{accountName ?? (isLocalDemo ? 'LOCAL DEMO' : authConfigured ? 'SIGN IN REQUIRED' : 'AUTH NOT CONFIGURED')}</span>
        {accountName && <button type="button" onClick={signOut}>Sign out</button>}
      </header>
      <section className="hero">
        <p className="eyebrow">YOUR ORGANIZATION'S KNOWLEDGE</p>
        <h1>Answers you can<br /><em>trace back to source.</em></h1>
        <p className="intro">Ask a question across your approved company documents. Responses include citations and evidence confidence when the API is configured.</p>
      </section>
      <section className="workspace" aria-label="Knowledge assistant">
        <form className="upload-form" onSubmit={uploadDocument}>
          <label htmlFor="document-file">Add knowledge documents</label>
          <div className="upload-row">
            <input id="document-file" type="file" accept=".pdf,.docx,.txt,.html,.htm" multiple onChange={(event) => setSelectedFiles(Array.from(event.currentTarget.files ?? []))} />
            <button type="submit" disabled={uploading || selectedFiles.length === 0 || (!getAccessToken && !isLocalDemo)}>{uploading ? 'Indexing…' : 'Upload & index'}</button>
          </div>
          <small>Select up to five files (20 MB each). They are indexed one at a time so progress and individual failures are clear. TXT, DOCX, HTML, and text-based PDFs work locally; scanned PDFs require Azure Document Intelligence for OCR.</small>
          {selectedFiles.length > 0 && <p className="upload-message">{selectedFiles.length} document{selectedFiles.length === 1 ? '' : 's'} selected.</p>}
          {uploadProgress && <p className="upload-message" role="status">{uploadProgress}</p>}
          {uploadResults.length > 0 && <ul className="upload-results">{uploadResults.map((result, index) => <li className={result.success ? 'upload-success' : 'upload-failure'} key={`${result.fileName}-${index}`}><strong>{result.fileName}</strong><span>{result.message}</span></li>)}</ul>}
        </form>
        <form className="composer" onSubmit={submit}>
          <label htmlFor="question">What would you like to know?</label>
          <textarea id="question" value={question} onChange={(event) => setQuestion(event.target.value)} placeholder="Ask a question about your organization's knowledge..." rows={3} required />
          <div className="composer-footer">
            <span>Answers are grounded in permitted sources.</span>
            <button type="submit" disabled={busy || !question.trim() || (!getAccessToken && !isLocalDemo)}>{busy ? 'Working…' : 'Ask question'} <span aria-hidden="true">↗</span></button>
          </div>
        </form>
        {error && <div className="notice" role="alert">{error}</div>}
        {busy && <div className="progress-card" role="status" aria-live="polite"><span className="progress-spinner" aria-hidden="true" /><div><strong>{workingStatus || 'Working on your request…'}</strong><p>{streamedAnswer || 'Please hold on while I search your documents and prepare an answer.'}</p></div></div>}
        {answer && <article className="answer-card"><div className="answer-title"><span>RESPONSE</span><span>Evidence confidence: {Math.round(answer.confidence * 100)}%</span></div><p>{answer.answer}</p><h2>Sources</h2><ul>{answer.citations.map((citation) => <li key={`${citation.documentId}-${citation.chunkId}`}><span>{citation.fileName}</span>{citation.pageNumber && <small>Page {citation.pageNumber}</small>}</li>)}</ul></article>}
        {!answer && !error && !busy && <div className="empty-state"><span className="spark">✳</span><p>{getAccessToken || isLocalDemo ? 'Your answers and source citations will appear here.' : 'Configure Entra ID to enable sign-in and queries.'}</p><small>{getAccessToken || isLocalDemo ? 'Upload documents above, then ask questions grounded in your indexed content.' : 'Set the public SPA client ID, tenant ID, and API scope in the Vite environment.'}</small></div>}
      </section>
      <footer>Private by design <span>•</span> Citations first <span>•</span> Human-verifiable answers</footer>
    </main>
  );
}

