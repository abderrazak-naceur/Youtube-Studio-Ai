import { StrictMode, useEffect, useMemo, useState } from 'react';
import { BarChart3, Check, ChevronRight, FileText, FolderOpen, LayoutDashboard, Loader2, Play, Settings, Sparkles, Upload, Video, X } from 'lucide-react';
import { OpportunityPanel } from './OpportunityPanel';
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

type ProjectSummary = {
  id: string;
  prompt: string;
  status: Stage;
  title?: string | null;
  updatedAtUtc: string;
};

type Artifact = {
  id: string;
  type: string;
  providerAssetId: string;
  content?: string | null;
  metadataJson?: string | null;
  createdAtUtc: string;
};

const configuredApiBase = import.meta.env.VITE_API_BASE_URL?.trim();
const API_BASE = configuredApiBase ? configuredApiBase.replace(/\/$/, '') : '';
const stages: Array<{ key: Stage; label: string; detail?: string }> = [
  { key: 'Researching', label: 'Research' },
  { key: 'Scripted', label: 'Script' },
  { key: 'Planned', label: 'Scene Plan' },
  { key: 'Producing', label: 'Production', detail: 'Visuals · Voice · Music/SFX · Captions' },
  { key: 'Rendering', label: 'Edit / Render' },
  { key: 'Qa', label: 'Quality Assurance' },
  { key: 'Completed', label: 'Professional MP4' },
];

const artifactLabels: Record<string, string> = {
  Research: 'Research', Script: 'Script', ScenePlan: 'Scene Plan', Visual: 'Visuals', Voice: 'Voice',
  MusicSfx: 'Music / SFX', Captions: 'Captions', Render: 'Edit / Render', Qa: 'Quality Assurance'
};

const navigation = [
  ['Dashboard', LayoutDashboard], ['Create Video', Video], ['My Videos', FolderOpen],
  ['Assets', Upload], ['Channels', Video], ['Analytics', BarChart3], ['Settings', Settings]
] as const;

function statusLabel(status: Stage) {
  if (status === 'Completed') return 'Completed';
  if (status === 'Failed') return 'Needs attention';
  if (status === 'Draft') return 'Draft';
  return 'In production';
}

function App() {
  const [prompt, setPrompt] = useState('');
  const [workspaceId, setWorkspaceId] = useState('');
  const [project, setProject] = useState<Project | null>(null);
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [artifacts, setArtifacts] = useState<Artifact[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [showCreate, setShowCreate] = useState(false);

  const currentIndex = useMemo(() => project ? stages.findIndex(stage => stage.key === project.status) : -1, [project]);

  async function loadProjects() {
    if (!workspaceId.trim()) return;
    try {
      const response = await fetch(`${API_BASE}/api/v1/video-projects?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
      if (response.ok) setProjects(await response.json());
    } catch {
      // Dashboard remains usable when the API is offline.
    }
  }

  useEffect(() => { void loadProjects(); }, [workspaceId]);

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
        // Keep the last known production state.
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
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: workspaceId.trim(), channelId: null, prompt: prompt.trim() })
      });
      if (!createResponse.ok) throw new Error(await createResponse.text());
      const created: Project = await createResponse.json();
      const startResponse = await fetch(`${API_BASE}/api/v1/video-projects/${created.id}/start`, { method: 'POST' });
      if (!startResponse.ok) throw new Error(await startResponse.text());
      setProject(await startResponse.json());
      setShowCreate(false);
      await loadProjects();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Impossibile avviare la produzione.');
    } finally { setBusy(false); }
  }

  return (
    <main className="min-h-screen bg-[#09090b] text-zinc-100">
      <div className="mx-auto flex min-h-screen max-w-[1440px]">
        <aside className="hidden w-60 shrink-0 border-r border-zinc-800/80 px-4 py-6 lg:block">
          <div className="flex items-center gap-2 px-3"><Sparkles size={18} /><span className="text-sm font-semibold">YouTube Studio AI</span></div>
          <nav className="mt-8 space-y-1">
            {navigation.map(([label, Icon], index) => (
              <button key={label} onClick={() => label === 'Create Video' && setShowCreate(true)} className={`flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm ${index === 0 ? 'bg-zinc-800 text-white' : 'text-zinc-500 hover:bg-zinc-900 hover:text-zinc-200'}`}>
                <Icon size={17} />{label}
              </button>
            ))}
          </nav>
        </aside>

        <section className="min-w-0 flex-1 px-5 py-7 sm:px-8 lg:px-10">
          <header className="flex items-center justify-between gap-4">
            <div><p className="text-xs font-semibold tracking-[0.25em] text-zinc-600">WORKSPACE</p><h1 className="mt-2 text-2xl font-semibold tracking-tight">Dashboard</h1></div>
            <button onClick={() => setShowCreate(true)} className="inline-flex items-center gap-2 rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-zinc-950 shadow-sm"><Video size={16} /> Create video</button>
          </header>

          <div className="mt-8 grid gap-5 xl:grid-cols-[1.25fr_.75fr]">
            <section className="overflow-hidden rounded-3xl border border-zinc-800 bg-gradient-to-br from-zinc-900 to-zinc-950 p-7">
              <p className="text-sm font-medium text-zinc-400">Turn an idea into a finished video</p>
              <h2 className="mt-3 max-w-xl text-3xl font-semibold leading-tight">One clear flow from prompt to professional MP4.</h2>
              <p className="mt-4 max-w-xl text-sm leading-6 text-zinc-500">Create, research, script, plan scenes, produce assets, render and run quality checks without leaving the workspace.</p>
              <button onClick={() => setShowCreate(true)} className="mt-7 inline-flex items-center gap-2 rounded-xl bg-zinc-100 px-5 py-3 text-sm font-semibold text-zinc-950">Start creating <ChevronRight size={16} /></button>
            </section>
            <section className="rounded-3xl border border-zinc-800 bg-zinc-900/50 p-6">
              <p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Production monitor</p>
              <div className="mt-5 flex items-end justify-between"><span className="text-3xl font-semibold">{projects.filter(p => !['Completed','Failed'].includes(p.status)).length}</span><span className="text-xs text-zinc-600">active projects</span></div>
              <div className="mt-5 h-2 overflow-hidden rounded-full bg-zinc-800"><div className="h-full w-2/3 rounded-full bg-zinc-300" /></div>
              <p className="mt-3 text-xs text-zinc-600">Track every stage of the production pipeline.</p>
            </section>
          </div>

          <section className="mt-7 rounded-3xl border border-zinc-800 bg-zinc-900/40 p-6">
            <div className="flex flex-wrap items-end justify-between gap-4">
              <div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Recent projects</p><h2 className="mt-1 text-xl font-semibold">Your latest work</h2></div>
              <label className="text-xs text-zinc-600">Workspace ID<input value={workspaceId} onChange={e => setWorkspaceId(e.target.value)} placeholder="UUID" className="ml-2 w-56 rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-300 outline-none" /></label>
            </div>
            {projects.length === 0 ? <div className="flex min-h-36 items-center justify-center text-sm text-zinc-600">Add a workspace ID to load recent projects.</div> : <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              {projects.map(item => <button key={item.id} onClick={() => setProject({ ...item, workspaceId, prompt: item.prompt })} className="rounded-2xl border border-zinc-800 bg-zinc-950/60 p-4 text-left hover:border-zinc-700">
                <div className="flex items-center justify-between gap-3"><span className="truncate text-sm font-medium">{item.title || item.prompt}</span><span className="shrink-0 text-[11px] text-zinc-600">{statusLabel(item.status)}</span></div>
                <p className="mt-2 line-clamp-2 text-xs leading-5 text-zinc-500">{item.prompt}</p><p className="mt-4 text-[11px] text-zinc-700">{new Date(item.updatedAtUtc).toLocaleString()}</p>
              </button>)}
            </div>}
          </section>

          <OpportunityPanel apiBase={API_BASE} workspaceId={workspaceId} />

          {project && <section className="mt-6 rounded-3xl border border-zinc-800 bg-zinc-900/50 p-6">
            <div className="flex items-center justify-between"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Production</p><h2 className="mt-1 text-xl font-semibold">{project.title || project.prompt}</h2></div><span className="rounded-full border border-zinc-700 px-3 py-1 text-xs text-zinc-400">{project.status}</span></div>
            <div className="mt-6 grid gap-6 lg:grid-cols-[.8fr_1.2fr]">
              <div className="space-y-3">{stages.map((stage, index) => { const done = currentIndex >= index && project.status !== 'Failed'; const active = project.status === stage.key; return <div key={stage.key} className="flex items-start gap-3 text-sm"><div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full border ${done ? 'border-zinc-300 bg-zinc-100 text-zinc-950' : 'border-zinc-700 text-zinc-600'}`}>{done && !active ? <Check size={15} /> : active ? <Loader2 size={15} className="animate-spin" /> : index + 1}</div><div><span className={done ? 'text-zinc-100' : 'text-zinc-500'}>{stage.label}</span>{stage.detail && <p className="mt-1 text-xs text-zinc-600">{stage.detail}</p>}</div></div>; })}</div>
              <div className="rounded-2xl border border-zinc-800 bg-zinc-950/60 p-5"><div className="flex items-center gap-2"><FileText size={17} /><span className="text-sm font-medium">Generated artifacts</span></div>{artifacts.length === 0 ? <p className="mt-5 text-sm text-zinc-600">Artifacts will appear here as production progresses.</p> : <div className="mt-4 grid gap-3 sm:grid-cols-2">{artifacts.map(artifact => <article key={artifact.id} className="rounded-xl border border-zinc-800 p-3"><p className="text-sm font-medium">{artifactLabels[artifact.type] ?? artifact.type}</p>{artifact.content ? <pre className="mt-2 max-h-32 overflow-auto whitespace-pre-wrap font-sans text-xs leading-5 text-zinc-500">{artifact.content}</pre> : <p className="mt-2 text-xs text-zinc-600">Asset ready: {artifact.providerAssetId}</p>}</article>)}</div>}</div>
            </div>
          </section>}
        </section>
      </div>

      {showCreate && <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm"><section className="w-full max-w-2xl rounded-3xl border border-zinc-800 bg-zinc-950 p-6 shadow-2xl"><div className="flex items-center justify-between"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">Create video</p><h2 className="mt-1 text-2xl font-semibold">Start with an idea</h2></div><button onClick={() => setShowCreate(false)} className="rounded-lg p-2 text-zinc-500 hover:bg-zinc-900"><X size={18} /></button></div><label className="mt-6 block text-sm font-medium">Workspace ID<input value={workspaceId} onChange={e => setWorkspaceId(e.target.value)} placeholder="UUID del workspace" className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none focus:border-zinc-600" /></label><label className="mt-5 block text-sm font-medium">What should we create?<textarea value={prompt} onChange={e => setPrompt(e.target.value)} rows={7} placeholder="Describe the video you want to make..." className="mt-2 w-full resize-none rounded-2xl border border-zinc-800 bg-zinc-900 px-4 py-4 text-sm leading-6 outline-none focus:border-zinc-600" /></label>{error && <p className="mt-3 text-sm text-red-400">{error}</p>}<button onClick={createVideo} disabled={busy} className="mt-5 inline-flex items-center gap-2 rounded-xl bg-white px-5 py-3 text-sm font-semibold text-zinc-950 disabled:opacity-50">{busy ? <Loader2 size={16} className="animate-spin" /> : <Play size={16} />}{busy ? 'Avvio...' : 'Create video'}{!busy && <ChevronRight size={16} />}</button></section></div>}
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
