import { StrictMode, useEffect, useMemo, useState } from 'react';
import { ArrowRight, Check, FileText, Loader2, Play, Sparkles } from 'lucide-react';
import './index.css';

type Stage = 'Draft' | 'Researching' | 'Scripted' | 'Planned' | 'Producing' | 'Rendering' | 'Qa' | 'Completed' | 'Failed';

type Project = {
  id: string;
  workspaceId: string;
  channelId?: string | null;
  prompt: string;
  status: Stage;
  title?: string | null;
  script?: string | null;
};

type Artifact = {
  id: string;
  type: string;
  providerAssetId: string;
  content?: string | null;
  metadataJson?: string | null;
  createdAtUtc: string;
};

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';
const stages: Stage[] = ['Researching', 'Scripted', 'Planned', 'Producing', 'Rendering', 'Qa', 'Completed'];

function App() {
  const [prompt, setPrompt] = useState('');
  const [workspaceId, setWorkspaceId] = useState('');
  const [project, setProject] = useState<Project | null>(null);
  const [artifacts, setArtifacts] = useState<Artifact[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const currentIndex = useMemo(() => project ? stages.indexOf(project.status) : -1, [project]);

  useEffect(() => {
    if (!project || project.status === 'Completed' || project.status === 'Failed') return;

    const refresh = async () => {
      try {
        const [projectResponse, artifactsResponse] = await Promise.all([
          fetch(`${API_BASE}/api/v1/video-projects/${project.id}`),
          fetch(`${API_BASE}/api/v1/video-projects/${project.id}/artifacts`)
        ]);
        if (projectResponse.ok) setProject(await projectResponse.json());
        if (artifactsResponse.ok) setArtifacts(await artifactsResponse.json());
      } catch {
        // The worker may be temporarily unavailable; keep the current UI state.
      }
    };

    void refresh();
    const timer = window.setInterval(refresh, 2000);
    return () => window.clearInterval(timer);
  }, [project?.id, project?.status]);

  async function createVideo() {
    setError('');
    if (!workspaceId.trim()) return setError('Inserisci il Workspace ID.');
    if (!prompt.trim()) return setError('Scrivi l’idea del video.');
    setBusy(true);
    setArtifacts([]);
    try {
      const createResponse = await fetch(`${API_BASE}/api/v1/video-projects`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId, channelId: null, prompt: prompt.trim() })
      });
      if (!createResponse.ok) throw new Error(await createResponse.text());
      const created: Project = await createResponse.json();
      const startResponse = await fetch(`${API_BASE}/api/v1/video-projects/${created.id}/start`, { method: 'POST' });
      if (!startResponse.ok) throw new Error(await startResponse.text());
      setProject(await startResponse.json());
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Impossibile avviare la produzione.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="min-h-screen bg-zinc-950 text-zinc-100">
      <div className="mx-auto max-w-6xl px-6 py-10">
        <header className="flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold tracking-[0.25em] text-zinc-500">YOUTUBE STUDIO AI</p>
            <h1 className="mt-2 text-3xl font-semibold tracking-tight">Create a professional video</h1>
            <p className="mt-2 text-sm text-zinc-400">Idea → research → script → production → professional MP4</p>
          </div>
          <Sparkles className="text-zinc-500" size={24} />
        </header>

        <section className="mt-10 grid gap-6 lg:grid-cols-[1.15fr_.85fr]">
          <div className="rounded-3xl border border-zinc-800 bg-zinc-900/60 p-6">
            <label className="text-sm font-medium">Workspace ID</label>
            <input value={workspaceId} onChange={e => setWorkspaceId(e.target.value)} placeholder="UUID del workspace" className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-950 px-4 py-3 text-sm outline-none focus:border-zinc-600" />

            <label className="mt-6 block text-sm font-medium">What should we create?</label>
            <textarea value={prompt} onChange={e => setPrompt(e.target.value)} rows={8} placeholder="Es. Crea un video di 8 minuti che spiega come l'AI sta cambiando il lavoro nel 2026..." className="mt-2 w-full resize-none rounded-2xl border border-zinc-800 bg-zinc-950 px-4 py-4 text-sm leading-6 outline-none focus:border-zinc-600" />
            {error && <p className="mt-3 text-sm text-red-400">{error}</p>}
            <button onClick={createVideo} disabled={busy} className="mt-5 inline-flex items-center gap-2 rounded-xl bg-white px-5 py-3 text-sm font-semibold text-zinc-950 disabled:cursor-not-allowed disabled:opacity-50">
              {busy ? <Loader2 size={16} className="animate-spin" /> : <Play size={16} />}
              {busy ? 'Avvio...' : 'Create video'}
              {!busy && <ArrowRight size={16} />}
            </button>
          </div>

          <div className="rounded-3xl border border-zinc-800 bg-zinc-900/60 p-6">
            <div className="flex items-center justify-between">
              <h2 className="font-medium">Production pipeline</h2>
              {project && <span className="rounded-full border border-zinc-700 px-3 py-1 text-xs text-zinc-400">{project.status}</span>}
            </div>
            {!project ? (
              <div className="flex min-h-64 items-center justify-center text-center text-sm text-zinc-500">Avvia un video per vedere la produzione in tempo reale.</div>
            ) : (
              <div className="mt-6 space-y-4">
                {stages.map((stage, index) => {
                  const done = currentIndex >= index && project.status !== 'Failed';
                  const active = project.status === stage;
                  return <div key={stage} className="flex items-center gap-3 text-sm">
                    <div className={`flex h-8 w-8 items-center justify-center rounded-full border ${done ? 'border-zinc-300 bg-zinc-100 text-zinc-950' : 'border-zinc-700 text-zinc-600'}`}>
                      {done && !active ? <Check size={15} /> : active ? <Loader2 size={15} className="animate-spin" /> : index + 1}
                    </div>
                    <span className={done ? 'text-zinc-100' : 'text-zinc-500'}>{stage === 'Qa' ? 'Quality Assurance' : stage}</span>
                  </div>;
                })}
              </div>
            )}
          </div>
        </section>

        {project && artifacts.length > 0 && (
          <section className="mt-6 rounded-3xl border border-zinc-800 bg-zinc-900/60 p-6">
            <div className="flex items-center gap-2">
              <FileText size={18} />
              <div>
                <p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Generated artifacts</p>
                <h2 className="mt-1 text-xl font-semibold">Production output</h2>
              </div>
            </div>
            <div className="mt-5 grid gap-4 md:grid-cols-2">
              {artifacts.map(artifact => (
                <article key={artifact.id} className="rounded-2xl border border-zinc-800 bg-zinc-950/70 p-4">
                  <div className="flex items-center justify-between gap-4">
                    <span className="text-sm font-medium">{artifact.type}</span>
                    <span className="text-xs text-zinc-500">{new Date(artifact.createdAtUtc).toLocaleString()}</span>
                  </div>
                  {artifact.content ? (
                    <pre className="mt-3 max-h-64 overflow-auto whitespace-pre-wrap font-sans text-sm leading-6 text-zinc-300">{artifact.content}</pre>
                  ) : (
                    <p className="mt-3 text-sm text-zinc-500">Asset pronto: {artifact.providerAssetId}</p>
                  )}
                </article>
              ))}
            </div>
          </section>
        )}
      </div>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
