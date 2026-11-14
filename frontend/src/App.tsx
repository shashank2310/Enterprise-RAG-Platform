import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { MsalProvider, useMsal } from '@azure/msal-react';
import { useState, type FormEvent } from 'react';
import { apiScope, authConfigured, msalInstance } from './authConfig';

type Citation = { documentId: string; fileName: string; pageNumber?: string };
type Answer = { answer: string; confidence: number; citations: Citation[] };
type WorkspaceProps = { getAccessToken?: () => Promise<string>; accountName?: string; onSignOut?: () => Promise<void> };

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5283';

export default function App() {
  if (!authConfigured || !msalInstance) {
    return <Workspace />;
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

function Workspace({ getAccessToken, accountName, onSignOut }: WorkspaceProps) {
  const [question, setQuestion] = useState('');
  const [answer, setAnswer] = useState<Answer | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setAnswer(null);
    setBusy(true);

    try {
      if (!getAccessToken) {
        throw new Error('Configure the Entra ID SPA client, tenant, and API scope before querying.');
      }
      const accessToken = await getAccessToken();
      const response = await fetch(`${apiBaseUrl}/api/rag/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ question }),
      });
      if (!response.ok) {
        const body = await response.json().catch(() => undefined);
        throw new Error(body?.detail ?? `Request failed (${response.status}).`);
      }
      setAnswer((await response.json()) as Answer);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The request could not be completed.');
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
        <span className="environment">{accountName ?? (authConfigured ? 'SIGN IN REQUIRED' : 'AUTH NOT CONFIGURED')}</span>
        {accountName && <button type="button" onClick={signOut}>Sign out</button>}
      </header>
      <section className="hero">
        <p className="eyebrow">YOUR ORGANIZATION'S KNOWLEDGE</p>
        <h1>Answers you can<br /><em>trace back to source.</em></h1>
        <p className="intro">Ask a question across your approved company documents. Responses include citations and evidence confidence when the API is configured.</p>
      </section>
      <section className="workspace" aria-label="Knowledge assistant">
        <form className="composer" onSubmit={submit}>
          <label htmlFor="question">What would you like to know?</label>
          <textarea id="question" value={question} onChange={(event) => setQuestion(event.target.value)} placeholder="Ask a question about your organization's knowledge..." rows={3} required />
          <div className="composer-footer">
            <span>Answers are grounded in permitted sources.</span>
            <button type="submit" disabled={busy || !question.trim() || !getAccessToken}>{busy ? 'Searching…' : 'Ask question'} <span aria-hidden="true">↗</span></button>
          </div>
        </form>
        {error && <div className="notice" role="alert">{error}</div>}
        {answer && <article className="answer-card"><div className="answer-title"><span>RESPONSE</span><span>Evidence confidence: {Math.round(answer.confidence * 100)}%</span></div><p>{answer.answer}</p><h2>Sources</h2><ul>{answer.citations.map((citation) => <li key={`${citation.documentId}-${citation.pageNumber}`}><span>{citation.fileName}</span>{citation.pageNumber && <small>Page {citation.pageNumber}</small>}</li>)}</ul></article>}
        {!answer && !error && <div className="empty-state"><span className="spark">✳</span><p>{getAccessToken ? 'Your answers and source citations will appear here.' : 'Configure Entra ID to enable sign-in and queries.'}</p><small>{getAccessToken ? 'The protected API returns HTTP 503 until orchestration and tenant data adapters are enabled.' : 'Set the public SPA client ID, tenant ID, and API scope in the Vite environment.'}</small></div>}
      </section>
      <footer>Private by design <span>•</span> Citations first <span>•</span> Human-verifiable answers</footer>
    </main>
  );
}

