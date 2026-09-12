import { useEffect, useState } from 'react';
import { CheckCircle2, CircleAlert, Plus, ShieldAlert } from 'lucide-react';

type Source = { id: string; title: string };
type Evidence = { id: string; quote: string; locator?: string | null; sourceId: string };
type Claim = { id: string; text: string; verificationStatus: string; evidenceIds: string[] };
type Props = { apiBase: string; workspaceId: string; researchProjectId: string; sources: Source[] };

const statuses = ['unverified', 'verified', 'disputed'] as const;

export function ResearchClaimsPanel({ apiBase, workspaceId, researchProjectId, sources }: Props) {
  const [evidence, setEvidence] = useState<Evidence[]>([]);
  const [claims, setClaims] = useState<Claim[]>([]);
  const [text, setText] = useState('');
  const [selectedEvidence, setSelectedEvidence] = useState<string[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    if (!workspaceId.trim() || !researchProjectId) return;
    setError('');
    try {
      const claimsResponse = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/claims?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
      if (!claimsResponse.ok) throw new Error(await claimsResponse.text());
      const claimPayload = await claimsResponse.json();
      setClaims((Array.isArray(claimPayload) ? claimPayload : []).map((claim: Partial<Claim>) => ({
        ...claim,
        evidenceIds: Array.isArray(claim.evidenceIds) ? claim.evidenceIds : []
      })) as Claim[]);
      const allEvidence: Evidence[] = [];
      for (const source of sources) {
        const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/sources/${source.id}/evidence?workspaceId=${encodeURIComponent(workspaceId.trim())}`);
        if (response.ok) {
          const payload = await response.json();
          if (Array.isArray(payload)) allEvidence.push(...payload.map((item: Omit<Evidence, 'sourceId'>) => ({ ...item, sourceId: source.id })));
        }
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

  async function updateVerification(claim: Claim, verificationStatus: typeof statuses[number]) {
    setBusy(true); setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/claims/${claim.id}/verification`, {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ workspaceId: workspaceId.trim(), verificationStatus })
      });
      if (!response.ok) throw new Error(await response.text());
      await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to update claim verification.'); }
    finally { setBusy(false); }
  }

  const sourceTitle = (sourceId: string) => sources.find(source => source.id === sourceId)?.title ?? 'Source';
  const evidenceById = new Map(evidence.map(item => [item.id, item]));

  return <section className="mt-5 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-5">
    <div className="flex items-center justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">Research provenance</p><h3 className="mt-1 text-lg font-semibold">Claims backed by evidence</h3></div><span className="text-xs text-zinc-600">{claims.length} claims</span></div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_1fr]"><div><label className="text-xs text-zinc-500">Claim<textarea value={text} onChange={event => setText(event.target.value)} rows={4} placeholder="State a factual claim that your research supports..." className="mt-2 w-full resize-none rounded-xl border border-zinc-800 bg-zinc-900 px-3 py-2.5 text-sm outline-none" /></label><p className="mt-3 text-xs text-zinc-600">Select the evidence that supports this claim.</p><div className="mt-2 max-h-48 space-y-2 overflow-auto">{evidence.map(item => <label key={item.id} className="flex gap-2 rounded-xl border border-zinc-800 p-3 text-xs"><input type="checkbox" checked={selectedEvidence.includes(item.id)} onChange={event => setSelectedEvidence(current => event.target.checked ? [...current, item.id] : current.filter(id => id !== item.id))} /><span><span className="block text-zinc-300">{item.quote}</span><span className="mt-1 block text-zinc-600">{sourceTitle(item.sourceId)}{item.locator ? ` · ${item.locator}` : ''}</span></span></label>)}{evidence.length === 0 && <p className="text-xs text-zinc-600">Add evidence to a source first.</p>}</div><button onClick={() => void createClaim()} disabled={busy || !researchProjectId} className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Plus size={14} /> Add claim</button></div>
      <div className="space-y-2">{claims.map(claim => <article key={claim.id} className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-4"><div className="flex items-start justify-between gap-3"><p className="text-sm leading-6 text-zinc-200">{claim.text}</p>{claim.verificationStatus === 'verified' ? <CheckCircle2 size={17} className="shrink-0" /> : claim.verificationStatus === 'disputed' ? <CircleAlert size={17} className="shrink-0" /> : <ShieldAlert size={17} className="shrink-0 text-zinc-600" />}</div><div className="mt-3 space-y-2">{claim.evidenceIds.map(id => { const item = evidenceById.get(id); return <div key={id} className="rounded-lg border border-zinc-800/80 px-3 py-2 text-[11px] text-zinc-500"><span className="font-medium text-zinc-400">{item ? sourceTitle(item.sourceId) : 'Evidence'}</span>{item?.locator && <span> · {item.locator}</span>}<p className="mt-1 line-clamp-2">{item?.quote ?? 'Evidence reference unavailable in the current source set.'}</p></div>; })}</div><div className="mt-3 flex flex-wrap items-center justify-between gap-2 text-[11px] text-zinc-600"><span>{claim.evidenceIds.length} evidence · {claim.verificationStatus}</span><div className="flex gap-1">{statuses.filter(status => status !== claim.verificationStatus).map(status => <button key={status} onClick={() => void updateVerification(claim, status)} disabled={busy} className="rounded-lg border border-zinc-700 px-2 py-1.5 text-zinc-300 hover:bg-zinc-800">Mark {status}</button>)}</div></div></article>)}{claims.length === 0 && <p className="rounded-xl border border-dashed border-zinc-800 p-8 text-center text-sm text-zinc-600">No claims yet.</p>}</div>
    </div>
  </section>;
}
