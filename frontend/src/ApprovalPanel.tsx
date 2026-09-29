import { useEffect, useState } from 'react';
import { CheckCircle2, DollarSign, ShieldCheck, ThumbsDown } from 'lucide-react';

type CostByStage = { stage: string; totalCostUsd: number };
type CostSummary = { totalCostUsd: number; byStage: CostByStage[] };

type Props = {
  apiBase: string;
  videoProjectId: string;
  onDecided?: (status: string) => void;
};

export function ApprovalPanel({ apiBase, videoProjectId, onDecided }: Props) {
  const [reviewer, setReviewer] = useState('');
  const [notes, setNotes] = useState('');
  const [costs, setCosts] = useState<CostSummary | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function loadCosts() {
      try {
        const response = await fetch(`${apiBase}/api/v1/video-projects/${videoProjectId}/costs`);
        if (!response.ok) return;
        const payload = await response.json() as CostSummary;
        if (!cancelled) setCosts(payload);
      } catch {
        // Cost preview is optional; the gate still works without it.
      }
    }
    void loadCosts();
    return () => { cancelled = true; };
  }, [apiBase, videoProjectId]);

  async function decide(decision: 'approve' | 'reject') {
    const trimmedReviewer = reviewer.trim();
    if (!trimmedReviewer) return setError('Enter a reviewer name before deciding.');
    if (decision === 'reject' && !notes.trim()) return setError('Rejection notes are required.');
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/video-projects/${videoProjectId}/${decision}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reviewer: trimmedReviewer, notes: notes.trim() || null })
      });
      if (!response.ok) throw new Error(await response.text());
      const payload = await response.json() as { status: string };
      onDecided?.(payload.status);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to record the decision.');
    } finally {
      setBusy(false);
    }
  }

  return <section className="mt-6 rounded-3xl border border-amber-800/40 bg-amber-950/10 p-6">
    <div className="flex items-center gap-2"><ShieldCheck size={18} className="text-amber-300" /><div><p className="text-xs font-semibold uppercase tracking-wider text-amber-500/80">Human quality gate</p><h3 className="mt-1 text-lg font-semibold">Approval required before export</h3></div></div>
    <p className="mt-2 text-sm leading-6 text-zinc-400">Automated QA passed. This video cannot be exported until a reviewer approves it. Rejecting sends it back with a recorded reason.</p>

    {costs && <div className="mt-4 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-4"><div className="flex items-center gap-2 text-sm font-medium"><DollarSign size={15} className="text-zinc-300" />Estimated production cost<span className="ml-auto text-base font-semibold">${costs.totalCostUsd.toFixed(4)}</span></div>{costs.byStage.length > 0 && <ul className="mt-3 grid gap-1 text-xs text-zinc-500 sm:grid-cols-2">{costs.byStage.map(stage => <li key={stage.stage} className="flex justify-between gap-3"><span>{stage.stage}</span><span>${stage.totalCostUsd.toFixed(4)}</span></li>)}</ul>}</div>}

    <div className="mt-4 grid gap-3">
      <label className="text-xs text-zinc-500">Reviewer<input value={reviewer} onChange={e => setReviewer(e.target.value)} placeholder="Your name" className="mt-1 w-full rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-sm text-zinc-200 outline-none focus:border-zinc-600" /></label>
      <label className="text-xs text-zinc-500">Notes <span className="text-zinc-700">(required to reject)</span><textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} placeholder="Optional approval notes or a required rejection reason..." className="mt-1 w-full resize-none rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-sm text-zinc-200 outline-none focus:border-zinc-600" /></label>
    </div>

    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}

    <div className="mt-4 flex flex-wrap gap-2">
      <button onClick={() => void decide('approve')} disabled={busy} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-4 py-2.5 text-sm font-semibold text-zinc-950 disabled:opacity-40"><CheckCircle2 size={15} />{busy ? 'Working...' : 'Approve & export'}</button>
      <button onClick={() => void decide('reject')} disabled={busy} className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-700 px-4 py-2.5 text-sm text-zinc-200 hover:bg-zinc-900 disabled:opacity-40"><ThumbsDown size={15} />Reject</button>
    </div>
  </section>;
}
