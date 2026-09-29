import { useEffect, useState, type ReactNode } from 'react';
import { FileText, Image, ListOrdered, PenLine, Sparkles, Tags } from 'lucide-react';

type Chapter = { timestamp: string; title: string };
type Draft = {
  angle: string;
  hook: string;
  outline: string;
  script: string;
  titleCandidates: string[];
  thumbnailConcepts: string[];
  description: string;
  chapters: Chapter[];
  tags: string[];
  status: string;
};
type Props = { apiBase: string; workspaceId: string; researchProjectId: string; onGenerated?: () => void };

function normalize(payload: Partial<Draft>): Draft {
  return {
    angle: typeof payload.angle === 'string' ? payload.angle : '',
    hook: typeof payload.hook === 'string' ? payload.hook : '',
    outline: typeof payload.outline === 'string' ? payload.outline : '',
    script: typeof payload.script === 'string' ? payload.script : '',
    titleCandidates: Array.isArray(payload.titleCandidates) ? payload.titleCandidates : [],
    thumbnailConcepts: Array.isArray(payload.thumbnailConcepts) ? payload.thumbnailConcepts : [],
    description: typeof payload.description === 'string' ? payload.description : '',
    chapters: Array.isArray(payload.chapters) ? payload.chapters : [],
    tags: Array.isArray(payload.tags) ? payload.tags : [],
    status: typeof payload.status === 'string' ? payload.status : 'draft'
  };
}

export function ContentDraftPanel({ apiBase, workspaceId, researchProjectId, onGenerated }: Props) {
  const [draft, setDraft] = useState<Draft | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId || !researchProjectId) { setDraft(null); return; }
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/content-draft?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
      if (response.ok) setDraft(normalize(await response.json()));
      else setDraft(null);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to load the content draft.');
    }
  }

  useEffect(() => { void load(); }, [workspaceId, researchProjectId]);

  async function generate() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId || !researchProjectId) return;
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/content-draft`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: scopedWorkspaceId })
      });
      if (!response.ok) throw new Error(await response.text());
      setDraft(normalize(await response.json()));
      onGenerated?.();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to generate content.');
    } finally {
      setBusy(false);
    }
  }

  return <section className="mt-5 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-5">
    <div className="flex flex-wrap items-start justify-between gap-3">
      <div>
        <p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">Content engine</p>
        <h3 className="mt-1 text-lg font-semibold">Editorial draft from verified research</h3>
        <p className="mt-1 text-xs text-zinc-500">Turns verified claims into an angle, hook, script, packaging and metadata.</p>
      </div>
      <button onClick={() => void generate()} disabled={busy || !researchProjectId} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Sparkles size={14} />{busy ? 'Generating...' : draft ? 'Regenerate' : 'Generate content'}</button>
    </div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    {!draft ? <p className="mt-4 rounded-xl border border-dashed border-zinc-800 p-8 text-center text-sm text-zinc-600">No content yet. Generate an editorial draft once the fact check is complete.</p> : <>
      <span className="mt-4 inline-flex items-center gap-1.5 rounded-lg border border-zinc-700 px-3 py-1.5 text-xs font-semibold">{draft.status === 'ready' ? 'Ready for production' : 'Draft — pending review'}</span>
      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        <Block icon={<PenLine size={15} />} title="Angle"><p className="text-sm leading-6 text-zinc-300">{draft.angle}</p></Block>
        <Block icon={<Sparkles size={15} />} title="Hook"><p className="text-sm leading-6 text-zinc-300">{draft.hook}</p></Block>
        <Block icon={<ListOrdered size={15} />} title="Outline"><pre className="whitespace-pre-wrap font-sans text-sm leading-6 text-zinc-300">{draft.outline}</pre></Block>
        <Block icon={<FileText size={15} />} title="Script"><pre className="max-h-52 overflow-auto whitespace-pre-wrap font-sans text-sm leading-6 text-zinc-300">{draft.script}</pre></Block>
        <Block icon={<PenLine size={15} />} title="Title candidates"><ul className="space-y-1.5 text-sm text-zinc-300">{draft.titleCandidates.map(title => <li key={title} className="rounded-lg border border-zinc-800 px-3 py-2">{title}</li>)}</ul></Block>
        <Block icon={<Image size={15} />} title="Thumbnail concepts"><ul className="space-y-1.5 text-sm text-zinc-300">{draft.thumbnailConcepts.map(concept => <li key={concept} className="rounded-lg border border-zinc-800 px-3 py-2">{concept}</li>)}</ul></Block>
        <Block icon={<FileText size={15} />} title="Description"><pre className="whitespace-pre-wrap font-sans text-sm leading-6 text-zinc-300">{draft.description}</pre></Block>
        <Block icon={<ListOrdered size={15} />} title="Chapters"><ul className="space-y-1 text-sm text-zinc-300">{draft.chapters.map(chapter => <li key={`${chapter.timestamp}-${chapter.title}`} className="flex gap-3"><span className="font-mono text-zinc-500">{chapter.timestamp}</span><span>{chapter.title}</span></li>)}</ul></Block>
      </div>
      <div className="mt-4 flex items-start gap-2"><Tags size={15} className="mt-1 shrink-0 text-zinc-500" /><div className="flex flex-wrap gap-1.5">{draft.tags.map(tag => <span key={tag} className="rounded-md border border-zinc-800 px-2 py-1 text-xs text-zinc-400">{tag}</span>)}</div></div>
    </>}
  </section>;
}

function Block({ icon, title, children }: { icon: ReactNode; title: string; children: ReactNode }) {
  return <div className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-4">
    <div className="flex items-center gap-2 text-xs font-medium text-zinc-400">{icon}{title}</div>
    <div className="mt-2">{children}</div>
  </div>;
}
