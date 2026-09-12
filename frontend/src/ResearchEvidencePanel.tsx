import { useEffect, useState } from 'react';
import { Plus, Quote } from 'lucide-react';

type Source = { id: string; title: string };
type Evidence = { id: string; quote: string; locator?: string | null; context?: string | null };
type Props = { apiBase: string; workspaceId: string; researchProjectId: string; sources: Source[] };

export function ResearchEvidencePanel({ apiBase, workspaceId, researchProjectId, sources }: Props) {
  const [sourceId, setSourceId] = useState('');
  const [evidence, setEvidence] = useState<Evidence[]>([]);
  const [quote, setQuote] = useState('');
  const [locator, setLocator] = useState('');
  const [context, setContext] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setSourceId(current => sources.some(source => source.id === current) ? current : (sources[0]?.id ?? ''));
  }, [sources]);

  async function load() {
    if (!workspaceId.trim() || !researchProjectId || !sourceId) { setEvidence([]); return; }
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/sources/${sourceId}/evidence?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
      if (!response.ok) throw new Error(await response.text());
      setEvidence(await response.json());
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to load evidence.'); }
  }

  useEffect(() => { void load(); }, [workspaceId, researchProjectId, sourceId]);

  async function addEvidence() {
    if (!sourceId || !quote.trim()) { setError('Select a source and enter the evidence quote.'); return; }
    setBusy(true); setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/sources/${sourceId}/evidence`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: workspaceId.trim(), researchSourceId: sourceId, quote: quote.trim(), locator: locator.trim() || null, context: context.trim() || null, metadataJson: null })
      });
      if (!response.ok) throw new Error(await response.text());
      setQuote(''); setLocator(''); setContext(''); await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to add evidence.'); }
    finally { setBusy(false); }
  }

  if (!sources.length) return <div className="mt-5 rounded-2xl border border-dashed border-zinc-800 p-4 text-sm text-zinc-600">Add a research source before capturing evidence.</div>;

  return <div className="mt-5 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-4">
    <div className="flex flex-wrap items-center justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Evidence</p><h3 className="mt-1 text-sm font-medium">Capture verifiable facts</h3></div><select aria-label="Evidence source" value={sourceId} onChange={event => setSourceId(event.target.value)} className="max-w-xs rounded-lg border border-zinc-800 bg-zinc-900 px-3 py-2 text-xs text-zinc-300 outline-none">{sources.map(source => <option key={source.id} value={source.id}>{source.title}</option>)}</select></div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    <div className="mt-4 grid gap-3 md:grid-cols-[1.4fr_.6fr]">
      <textarea aria-label="Evidence quote" value={quote} onChange={event => setQuote(event.target.value)} rows={3} placeholder="Exact quote or factual evidence..." className="w-full resize-none rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-sm text-zinc-200 outline-none" />
      <div className="space-y-3"><input aria-label="Evidence locator" value={locator} onChange={event => setLocator(event.target.value)} placeholder="Page / timestamp / section" className="w-full rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-xs text-zinc-200 outline-none" /><input aria-label="Evidence context" value={context} onChange={event => setContext(event.target.value)} placeholder="Context (optional)" className="w-full rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-xs text-zinc-200 outline-none" /><button onClick={() => void addEvidence()} disabled={busy} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Plus size={14} /> Add evidence</button></div>
    </div>
    {evidence.length === 0 ? <p className="mt-4 text-xs text-zinc-600">No evidence captured for this source yet.</p> : <div className="mt-4 space-y-2">{evidence.map(item => <article key={item.id} className="rounded-xl border border-zinc-800 p-3"><div className="flex gap-2"><Quote size={14} className="mt-0.5 shrink-0 text-zinc-600" /><p className="text-xs leading-5 text-zinc-400">{item.quote}</p></div>{item.locator && <p className="mt-2 text-[11px] text-zinc-600">{item.locator}</p>}{item.context && <p className="mt-1 text-[11px] leading-4 text-zinc-600">{item.context}</p>}</article>)}</div>}
  </div>;
}
