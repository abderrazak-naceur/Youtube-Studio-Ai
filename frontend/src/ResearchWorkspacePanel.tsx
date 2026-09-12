import { useEffect, useMemo, useState } from 'react';
import { ExternalLink, FilePlus2, Plus, Trash2 } from 'lucide-react';

type Opportunity = { id: string; title: string };
type ResearchProject = { id: string; opportunityId: string; status: string };
type ResearchSource = { id: string; url: string; title: string; metadataJson: string };

type Props = { apiBase: string; workspaceId: string };

export function ResearchWorkspacePanel({ apiBase, workspaceId }: Props) {
  const [opportunities, setOpportunities] = useState<Opportunity[]>([]);
  const [projects, setProjects] = useState<ResearchProject[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState('');
  const [selectedOpportunityId, setSelectedOpportunityId] = useState('');
  const [sources, setSources] = useState<ResearchSource[]>([]);
  const [title, setTitle] = useState('');
  const [url, setUrl] = useState('');
  const [metadataJson, setMetadataJson] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const opportunityById = useMemo(
    () => new Map(opportunities.map(opportunity => [opportunity.id, opportunity.title])),
    [opportunities]
  );
  const availableOpportunities = useMemo(
    () => opportunities.filter(opportunity => !projects.some(project => project.opportunityId === opportunity.id)),
    [opportunities, projects]
  );

  async function loadProjects() {
    if (!workspaceId.trim()) return;
    setError('');
    try {
      const [projectsResponse, opportunitiesResponse] = await Promise.all([
        fetch(`${apiBase}/api/v1/research-projects?workspaceId=${encodeURIComponent(workspaceId.trim())}`),
        fetch(`${apiBase}/api/v1/opportunities?workspaceId=${encodeURIComponent(workspaceId.trim())}&sort=opportunityScore&direction=desc`)
      ]);
      if (!projectsResponse.ok) throw new Error(await projectsResponse.text());
      if (!opportunitiesResponse.ok) throw new Error(await opportunitiesResponse.text());
      const nextProjects: ResearchProject[] = await projectsResponse.json();
      setProjects(nextProjects);
      setOpportunities(await opportunitiesResponse.json());
      setSelectedProjectId(current => nextProjects.some(project => project.id === current) ? current : (nextProjects[0]?.id ?? ''));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to load research projects.');
    }
  }

  async function loadSources(projectId: string) {
    if (!workspaceId.trim() || !projectId) {
      setSources([]);
      return;
    }
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${projectId}/sources?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
      if (!response.ok) throw new Error(await response.text());
      setSources(await response.json());
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to load research sources.');
    }
  }

  useEffect(() => { void loadProjects(); }, [workspaceId]);
  useEffect(() => { void loadSources(selectedProjectId); }, [workspaceId, selectedProjectId]);

  async function createProject() {
    if (!selectedOpportunityId) return setError('Choose an opportunity before starting research.');
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: workspaceId.trim(), opportunityId: selectedOpportunityId })
      });
      if (!response.ok) throw new Error(await response.text());
      const project: ResearchProject = await response.json();
      setSelectedOpportunityId('');
      await loadProjects();
      setSelectedProjectId(project.id);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to start research.');
    } finally {
      setBusy(false);
    }
  }

  async function addSource() {
    if (!selectedProjectId || !title.trim() || !url.trim()) return setError('Source title and URL are required.');
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${selectedProjectId}/sources`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: workspaceId.trim(), title: title.trim(), url: url.trim(), metadataJson: metadataJson.trim() || null })
      });
      if (!response.ok) throw new Error(await response.text());
      setTitle('');
      setUrl('');
      setMetadataJson('');
      await loadSources(selectedProjectId);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to add research source.');
    } finally {
      setBusy(false);
    }
  }

  async function removeSource(source: ResearchSource) {
    if (!selectedProjectId || !window.confirm(`Delete “${source.title}”?`)) return;
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${selectedProjectId}/sources/${source.id}?workspaceId=${encodeURIComponent(workspaceId.trim())}`, { method: 'DELETE' });
      if (!response.ok) throw new Error(await response.text());
      await loadSources(selectedProjectId);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to delete research source.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mt-6 rounded-3xl border border-zinc-800 bg-zinc-900/40 p-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Research workspace</p><h2 className="mt-1 text-xl font-semibold">Evidence before production</h2></div>
        {workspaceId.trim() && <div className="flex flex-wrap gap-2"><select aria-label="Opportunity for research" value={selectedOpportunityId} onChange={event => setSelectedOpportunityId(event.target.value)} className="rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-300 outline-none"><option value="">Choose an opportunity</option>{availableOpportunities.map(opportunity => <option key={opportunity.id} value={opportunity.id}>{opportunity.title}</option>)}</select><button onClick={() => void createProject()} disabled={busy || !selectedOpportunityId} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><FilePlus2 size={14} /> Start research</button></div>}
      </div>

      {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
      {!workspaceId.trim() ? <div className="mt-5 flex min-h-28 items-center justify-center text-sm text-zinc-600">Enter a Workspace ID above to manage research.</div> : projects.length === 0 ? <div className="mt-5 flex min-h-28 items-center justify-center text-sm text-zinc-600">Start research from a content opportunity to collect evidence.</div> : <>
        <label className="mt-5 block text-xs text-zinc-500">Research project<select aria-label="Research project" value={selectedProjectId} onChange={event => setSelectedProjectId(event.target.value)} className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-950 px-3 py-2.5 text-sm text-zinc-300 outline-none">{projects.map(project => <option key={project.id} value={project.id}>{opportunityById.get(project.opportunityId) ?? project.opportunityId} · {project.status}</option>)}</select></label>
        <div className="mt-5 grid gap-5 lg:grid-cols-[1.1fr_.9fr]">
          <div>{sources.length === 0 ? <div className="flex min-h-32 items-center justify-center rounded-2xl border border-dashed border-zinc-800 text-sm text-zinc-600">No sources yet. Add the evidence you want to use.</div> : <div className="space-y-2">{sources.map(source => <article key={source.id} className="flex items-start justify-between gap-3 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-4"><div className="min-w-0"><h3 className="truncate text-sm font-medium">{source.title}</h3><a href={source.url} target="_blank" rel="noreferrer" className="mt-2 inline-flex max-w-full items-center gap-1 truncate text-xs text-zinc-500 hover:text-zinc-300"><ExternalLink size={12} />{source.url}</a></div><button aria-label={`Delete ${source.title}`} onClick={() => void removeSource(source)} disabled={busy} className="rounded-md p-1.5 text-zinc-600 hover:bg-zinc-900 hover:text-red-300"><Trash2 size={14} /></button></article>)}</div>}</div>
          <div className="rounded-2xl border border-zinc-800 bg-zinc-950/60 p-4"><p className="text-sm font-medium">Add a source</p><label className="mt-4 block text-xs text-zinc-500">Title<input aria-label="Source title" value={title} onChange={event => setTitle(event.target.value)} className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-sm text-zinc-200 outline-none" /></label><label className="mt-3 block text-xs text-zinc-500">URL<input aria-label="Source URL" type="url" value={url} onChange={event => setUrl(event.target.value)} placeholder="https://" className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-sm text-zinc-200 outline-none" /></label><label className="mt-3 block text-xs text-zinc-500">Metadata JSON optional<textarea aria-label="Source metadata" value={metadataJson} onChange={event => setMetadataJson(event.target.value)} rows={3} placeholder='{ "author": "..." }' className="mt-2 w-full resize-none rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 font-mono text-xs text-zinc-200 outline-none" /></label><button onClick={() => void addSource()} disabled={busy || !selectedProjectId} className="mt-4 inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Plus size={14} /> Add source</button></div>
        </div>
      </>}
    </section>
  );
}
