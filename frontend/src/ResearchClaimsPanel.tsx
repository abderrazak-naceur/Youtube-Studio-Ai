import { useEffect, useState } from 'react';
import { CheckCircle2, Plus, ShieldAlert } from 'lucide-react';

type Source = { id: string; title: string };
type Evidence = { id: string; quote: string; locator?: string | null };
type Claim = { id: string; text: string; verificationStatus: string; evidenceIds: string[] };
type Props = { apiBase: string; workspaceId: string; researchProjectId: string; sources: Source[] };

export function ResearchClaimsPanel({ apiBase, workspaceId, researchProjectId, sources }: Props) {
  const [evidence, setEvidence] = useState<Evidence[]>([]);
  const [claims, setClaims] = useState<Claim[]>([]);
  const [text, setText] = useState('');
  const [selectedEvidence, setSelectedEvidence] = useState<string[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    if (!workspaceId.trim() || !researchProjectId) return;
    try {
      const claimsResponse = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/claims?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
      if (!claimsResponse.ok) throw new Error(await claimsResponse.text());
      setClaims(await claimsResponse.json());
      const allEvidence: Evidence[] = [];
      for (const source of sources) {
        const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/sources/${source.id}/evidence?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
        if (response.ok) allEvidence.push(...await response.json());
      }
      setEvidence(allEvidence);
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to load claims.'); }
  }

  useEffect(() => { void load(); }, [workspaceId, researchProjectId, sources.map(source => source.id).join(',')]);

  async function createClaim() {
    if (!text.trim()) return setError('Claim text is required.');
    if (selectedEvidence.length === 0) return setError('Select at least one evidence record.');
    setBusy(true); setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/claims`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: workspaceId.trim(), text: text.trim(), evidenceIds: selectedEvidence, verificationStatus: 'unverified' })
      });
      if (!response.ok) throw new Error(await response.text());
      setText(''); setSelectedEvidence([]); await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to create claim.'); }
    finally { setBusy(false); }
  }

  async function verify(claim: Claim) {
    setBusy(true); setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/claims/${claim.id}/verification`, {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ workspaceId: workspaceId.trim(), verificationStatus: 'verified' })
      });
      if (!response.ok) throw new Error(await response.text());
      await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to verify claim.'); }
    finally { setBusy(false); }
  }

  return <section className="mt-5 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-5">
    <div className="flex items-center justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">Research provenance</p><h3 className="mt-1 text-lg font-semibold">Claims backed by evidence</h3></div><span className="text-xs text-zinc-600">{claims.length} claims</span></div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_1fr]"><div><label className="text-xs text-zinc-500">Claim<textarea value={text} onChange={event => setText(event.target.value)} rows={4} placeholder="State a factual claim that your research supports..." className="mt-2 w-full resize-none rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-sm outline-none" /></label><p className="mt-3 text-xs text-zinc-600">Select the evidence that supports this claim.</p><div className="mt-2 max-h-48 space-y-2 overflow-auto">{evidence.map(item => <label key={item.id} className="flex gap-2 rounded-xl border border-zinc-800 p-3 text-xs"><input type="checkbox" checked={selectedEvidence.includes(item.id)} onChange={event => setSelectedEvidence(current => event.target.checked ? [...current, item.id] : current.filter(id => id !== item.id))} /><span><span className="block text-zinc-300">{item.quote}</span>{item.locator && <span className="mt-1 block text-zinc-600">{item.locator}</span>}</span></label>)}{evidence.length === 0 && <p className="text-xs text-zinc-600">Add evidence to a source first.</p>}</div><button onClick={() => void createClaim()} disabled={busy || !researchProjectId} className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Plus size={14} /> Add claim</button></div>
      <div className="space-y-2">{claims.map(claim => <article key={claim.id} className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-4"><div className="flex items-start justify-between gap-3"><p className="text-sm leading-6 text-zinc-200">{claim.text}</p>{claim.verificationStatus === 'verified' ? <CheckCircle2 size={17} className="shrink-0" /> : <ShieldAlert size={17} className="shrink-0 text-zinc-600" />}</div><div className="mt-3 flex items-center justify-between text-[11px] text-zinc-600"><span>{claim.evidenceIds.length} evidence · {claim.verificationStatus}</span>{claim.verificationStatus !== 'verified' && <button onClick={() => void verify(claim)} disabled={busy} className="rounded-lg border border-zinc-700 px-2.5 py-1.5 text-zinc-300 hover:bg-zinc-800">Mark verified</button>}</div></article>)}{claims.length === 0 && <p className="rounded-xl border border-dashed border-zinc-800 p-8 text-center text-sm text-zinc-600">No claims yet.</p>}</div>
    </div>
  </section>;
}
