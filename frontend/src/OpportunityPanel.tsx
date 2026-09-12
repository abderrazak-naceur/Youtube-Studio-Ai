import { useEffect, useState } from 'react';
import { ChevronDown, Pencil, Plus, Trash2, X } from 'lucide-react';

type Opportunity = {
  id: string;
  title: string;
  status: string;
  opportunityScore: number;
  revenueScore: number;
  audienceProblem?: string | null;
  rationale?: string | null;
};

type Props = { apiBase: string; workspaceId: string };

const statuses = ['New', 'Validated', 'Selected', 'Rejected'];

export function OpportunityPanel({ apiBase, workspaceId }: Props) {
  const [items, setItems] = useState<Opportunity[]>([]);
  const [status, setStatus] = useState('');
  const [sort, setSort] = useState<'opportunityScore' | 'revenueScore'>('opportunityScore');
  const [direction, setDirection] = useState<'asc' | 'desc'>('desc');
  const [editing, setEditing] = useState<Opportunity | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const [title, setTitle] = useState('');
  const [opportunityScore, setOpportunityScore] = useState(70);
  const [revenueScore, setRevenueScore] = useState(70);
  const [audienceProblem, setAudienceProblem] = useState('');
  const [rationale, setRationale] = useState('');
  const [formOpen, setFormOpen] = useState(false);

  async function load() {
    if (!workspaceId.trim()) return;
    setError('');
    try {
      const params = new URLSearchParams({ workspaceId: workspaceId.trim(), sort, direction });
      if (status) params.set('status', status);
      const response = await fetch(`${apiBase}/api/v1/opportunities?${params}`);
      if (!response.ok) throw new Error(await response.text());
      setItems(await response.json());
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load opportunities.');
    }
  }

  useEffect(() => { void load(); }, [workspaceId, status, sort, direction]);

  function resetForm() {
    setEditing(null);
    setTitle(''); setOpportunityScore(70); setRevenueScore(70);
    setAudienceProblem(''); setRationale(''); setFormOpen(false);
  }

  function startEdit(item: Opportunity) {
    setEditing(item); setTitle(item.title); setOpportunityScore(item.opportunityScore);
    setRevenueScore(item.revenueScore); setAudienceProblem(item.audienceProblem ?? '');
    setRationale(item.rationale ?? ''); setFormOpen(true);
  }

  async function save() {
    if (!workspaceId.trim() || !title.trim()) return setError('Workspace ID and title are required.');
    if (opportunityScore < 0 || opportunityScore > 100 || revenueScore < 0 || revenueScore > 100)
      return setError('Scores must be between 0 and 100.');
    setBusy(true); setError('');
    try {
      const response = await fetch(editing
        ? `${apiBase}/api/v1/opportunities/${editing.id}`
        : `${apiBase}/api/v1/opportunities`, {
        method: editing ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          workspaceId: workspaceId.trim(), title: title.trim(),
          status: editing?.status ?? 'New', opportunityScore, revenueScore,
          audienceProblem: audienceProblem.trim() || null, rationale: rationale.trim() || null
        })
      });
      if (!response.ok) throw new Error(await response.text());
      resetForm(); await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to save opportunity.');
    } finally { setBusy(false); }
  }

  async function remove(item: Opportunity) {
    if (!window.confirm(`Delete “${item.title}”?`)) return;
    setBusy(true); setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/opportunities/${item.id}?workspaceId=${encodeURIComponent(workspaceId.trim())}`, { method: 'DELETE' });
      if (!response.ok) throw new Error(await response.text());
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to delete opportunity.');
    } finally { setBusy(false); }
  }

  return (
    <section className="mt-6 rounded-3xl border border-zinc-800 bg-zinc-900/40 p-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Opportunity engine</p><h2 className="mt-1 text-xl font-semibold">Ideas worth producing</h2></div>
        <div className="flex flex-wrap items-center gap-2">
          <select value={status} onChange={e => setStatus(e.target.value)} className="rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-400 outline-none">
            <option value="">All statuses</option>{statuses.map(value => <option key={value}>{value}</option>)}
          </select>
          <select value={sort} onChange={e => setSort(e.target.value as typeof sort)} className="rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-400 outline-none">
            <option value="opportunityScore">Opportunity score</option><option value="revenueScore">Revenue score</option>
          </select>
          <button onClick={() => setDirection(value => value === 'desc' ? 'asc' : 'desc')} className="rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-400">{direction === 'desc' ? 'High → low' : 'Low → high'}</button>
          <button onClick={() => { resetForm(); setFormOpen(true); }} disabled={!workspaceId.trim()} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Plus size={14} /> Add opportunity</button>
        </div>
      </div>

      {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
      {!workspaceId.trim() ? <div className="mt-5 flex min-h-28 items-center justify-center text-sm text-zinc-600">Enter a Workspace ID above to manage opportunities.</div> : items.length === 0 ? <div className="mt-5 flex min-h-28 items-center justify-center text-sm text-zinc-600">No opportunities yet. Add the first content idea.</div> : <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {items.map(item => <article key={item.id} className="rounded-2xl border border-zinc-800 bg-zinc-950/60 p-4">
          <div className="flex items-start justify-between gap-3"><div><h3 className="text-sm font-medium">{item.title}</h3><span className="mt-1 inline-block text-[11px] text-zinc-600">{item.status}</span></div><div className="flex gap-1"><button onClick={() => startEdit(item)} className="rounded-md p-1.5 text-zinc-600 hover:bg-zinc-900 hover:text-zinc-200" aria-label="Edit opportunity"><Pencil size={14} /></button><button onClick={() => void remove(item)} disabled={busy} className="rounded-md p-1.5 text-zinc-600 hover:bg-zinc-900 hover:text-red-300" aria-label="Delete opportunity"><Trash2 size={14} /></button></div></div>
          <div className="mt-4 grid grid-cols-2 gap-2"><div className="rounded-xl bg-zinc-900 p-3"><p className="text-[10px] uppercase tracking-wider text-zinc-600">Opportunity</p><p className="mt-1 text-lg font-semibold">{item.opportunityScore}</p></div><div className="rounded-xl bg-zinc-900 p-3"><p className="text-[10px] uppercase tracking-wider text-zinc-600">Revenue</p><p className="mt-1 text-lg font-semibold">{item.revenueScore}</p></div></div>
          {item.audienceProblem && <p className="mt-3 text-xs leading-5 text-zinc-500"><strong className="font-medium text-zinc-400">Audience:</strong> {item.audienceProblem}</p>}
          {item.rationale && <p className="mt-2 text-xs leading-5 text-zinc-500"><strong className="font-medium text-zinc-400">Why:</strong> {item.rationale}</p>}
        </article>)}
      </div>}

      {formOpen && <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm"><section className="w-full max-w-xl rounded-3xl border border-zinc-800 bg-zinc-950 p-6 shadow-2xl">
        <div className="flex items-center justify-between"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">{editing ? 'Edit opportunity' : 'New opportunity'}</p><h3 className="mt-1 text-xl font-semibold">Content opportunity</h3></div><button onClick={resetForm} className="rounded-lg p-2 text-zinc-500 hover:bg-zinc-900"><X size={18} /></button></div>
        <label className="mt-5 block text-sm font-medium">Title<input value={title} onChange={e => setTitle(e.target.value)} className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none focus:border-zinc-600" /></label>
        <div className="mt-4 grid grid-cols-2 gap-3"><label className="text-sm font-medium">Opportunity score<input type="number" min="0" max="100" value={opportunityScore} onChange={e => setOpportunityScore(Number(e.target.value))} className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none" /></label><label className="text-sm font-medium">Revenue score<input type="number" min="0" max="100" value={revenueScore} onChange={e => setRevenueScore(Number(e.target.value))} className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none" /></label></div>
        {editing && <label className="mt-4 block text-sm font-medium">Status<select value={editing.status} onChange={e => setEditing({ ...editing, status: e.target.value })} className="mt-2 w-full rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none">{statuses.map(value => <option key={value}>{value}</option>)}</select></label>}
        <label className="mt-4 block text-sm font-medium">Audience problem<textarea value={audienceProblem} onChange={e => setAudienceProblem(e.target.value)} rows={3} className="mt-2 w-full resize-none rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none" /></label>
        <label className="mt-4 block text-sm font-medium">Rationale<textarea value={rationale} onChange={e => setRationale(e.target.value)} rows={3} className="mt-2 w-full resize-none rounded-xl border border-zinc-800 bg-zinc-900 px-4 py-3 text-sm outline-none" /></label>
        <button onClick={() => void save()} disabled={busy} className="mt-5 inline-flex items-center gap-2 rounded-xl bg-white px-5 py-3 text-sm font-semibold text-zinc-950 disabled:opacity-50">{busy ? 'Saving…' : 'Save opportunity'}<ChevronDown size={15} className="rotate-[-90deg]" /></button>
      </section></div>}
    </section>
  );
}
