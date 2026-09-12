import { FormEvent, useEffect, useState } from 'react';
import { ExternalLink, Plus, RefreshCw } from 'lucide-react';

type ResearchProject = { id: string; opportunityId: string; workspaceId: string; status: string };
type ResearchSource = { id: string; url: string; title: string; metadataJson?: string | null; createdAtUtc?: string };
type Props = { apiBase: string; workspaceId: string; opportunityId?: string | null };

export function ResearchSourcesPanel({ apiBase, workspaceId, opportunityId }: Props) {
  const [project, setProject] = useState<ResearchProject | null>(null);
  const [sources, setSources] = useState<ResearchSource[]>([]);
  const [url, setUrl] = useState('');
  const [title, setTitle] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId) return;
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
      if (!response.ok) throw new Error(await response.text());
      const projects: ResearchProject[] = await response.json();
      const selected = projects.find(item => !opportunityId || item.opportunityId === opportunityId) ?? projects[0] ?? null;
      setProject(selected);
      if (!selected) return setSources([]);
      const sourceResponse = await fetch(`${apiBase}/api/v1/research-projects/${selected.id}/sources?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
      if (!sourceResponse.ok) throw new Error(await sourceResponse.text());
      setSources(await sourceResponse.json());
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to load research sources.'); }
  }

  useEffect(() => { void load(); }, [workspaceId, opportunityId]);

  async function addSource(event: FormEvent) {
    event.preventDefault();
    const scopedWorkspaceId = workspaceId.trim();
    if (!project) return setError('Create a research project before adding sources.');
    if (!scopedWorkspaceId) return setError('Workspace ID is required.');
    if (!url.trim() || !title.trim()) return setError('URL and title are required.');
    setBusy(true); setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${project.id}/sources`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: scopedWorkspaceId, researchProjectId: project.id, url: url.trim(), title: title.trim(), metadataJson: '{}' })
      });
      if (!response.ok) throw new Error(await response.text());
      setUrl(''); setTitle(''); await load();
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to add source.'); }
    finally { setBusy(false); }
  }

  return <section className="mt-6 rounded-3xl border border-zinc-800 bg-zinc-900/40 p-6">
    <div className="flex items-center justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Research engine</p><h2 className="mt-1 text-xl font-semibold">Research sources</h2></div><button onClick={() => void load()} className="inline-flex items-center gap-2 rounded-lg border border-zinc-800 px-3 py-2 text-xs text-zinc-400"><RefreshCw size={14} /> Refresh</button></div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    {!workspaceId.trim() ? <p className="mt-5 text-sm text-zinc-600">Enter a Workspace ID to load research.</p> : !project ? <p className="mt-5 text-sm text-zinc-600">No research project is available yet.</p> : <>
      <p className="mt-3 text-xs text-zinc-600">Project status: {project.status}</p>
      <form onSubmit={addSource} className="mt-5 grid gap-3 md:grid-cols-[1fr_1fr_auto]"><input value={title} onChange={e => setTitle(e.target.value)} placeholder="Source title" className="rounded-xl border border-zinc-800 bg-zinc-950 px-3 py-2.5 text-sm outline-none" /><input value={url} onChange={e => setUrl(e.target.value)} placeholder="https://example.com/article" type="url" className="rounded-xl border border-zinc-800 bg-zinc-950 px-3 py-2.5 text-sm outline-none" /><button disabled={busy} className="inline-flex items-center justify-center gap-2 rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-zinc-950 disabled:opacity-50"><Plus size={15} /> Add</button></form>
      {sources.length === 0 ? <p className="mt-5 text-sm text-zinc-600">No sources added yet.</p> : <div className="mt-5 space-y-2">{sources.map(source => <article key={source.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-zinc-800 bg-zinc-950/60 p-3"><div className="min-w-0"><p className="text-sm font-medium">{source.title}</p><p className="mt-1 truncate text-xs text-zinc-600">{source.url}</p></div><a href={source.url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-xs text-zinc-400">Open <ExternalLink size={13} /></a></article>)}</div>}
    </>}
  </section>;
}
