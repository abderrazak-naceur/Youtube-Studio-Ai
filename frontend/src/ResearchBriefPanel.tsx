import { useEffect, useMemo, useState } from 'react';
import { CheckCircle2, Clipboard, FileText, Save, ShieldAlert } from 'lucide-react';

type Source = { id: string; title: string; url: string };
type Evidence = { id: string; quote: string; locator?: string | null; sourceId: string };
type Claim = { id: string; text: string; verificationStatus: string; evidenceIds: string[] };

type SavedBrief = { markdown: string; status: string; pendingClaimCount: number };
type Props = {
  apiBase: string;
  workspaceId: string;
  researchProjectId: string;
  projectTitle: string;
  sources: Source[];
  onSaved?: () => void;
};

export function ResearchBriefPanel({ apiBase, workspaceId, researchProjectId, projectTitle, sources, onSaved }: Props) {
  const [claims, setClaims] = useState<Claim[]>([]);
  const [evidence, setEvidence] = useState<Evidence[]>([]);
  const [savedBrief, setSavedBrief] = useState<SavedBrief | null>(null);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const [busy, setBusy] = useState(false);

  async function load() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId || !researchProjectId) return;
    setError('');
    try {
      const claimsResponse = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/claims?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
      if (!claimsResponse.ok) throw new Error(await claimsResponse.text());
      const claimPayload = await claimsResponse.json();
      setClaims((Array.isArray(claimPayload) ? claimPayload : []).map((claim: Partial<Claim>) => ({
        ...claim,
        evidenceIds: Array.isArray(claim.evidenceIds) ? claim.evidenceIds : []
      })) as Claim[]);

      const evidenceGroups = await Promise.all(sources.map(async source => {
        const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/sources/${source.id}/evidence?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
        if (!response.ok) return [] as Evidence[];
        const payload = await response.json();
        return Array.isArray(payload) ? payload.map((item: Omit<Evidence, 'sourceId'>) => ({ ...item, sourceId: source.id })) : [];
      }));
      setEvidence(evidenceGroups.flat());

      const briefResponse = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/brief?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
      if (briefResponse.ok) {
        const brief = await briefResponse.json() as Partial<SavedBrief>;
        setSavedBrief({
          markdown: typeof brief.markdown === 'string' ? brief.markdown : '',
          status: typeof brief.status === 'string' ? brief.status : 'draft',
          pendingClaimCount: typeof brief.pendingClaimCount === 'number' ? brief.pendingClaimCount : 0
        });
      } else {
        setSavedBrief(null);
      }
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to generate the research brief.');
    }
  }

  useEffect(() => { void load(); }, [workspaceId, researchProjectId, sources.map(source => source.id).join(',')]);

  const evidenceById = useMemo(() => new Map(evidence.map(item => [item.id, item])), [evidence]);
  const sourceById = useMemo(() => new Map(sources.map(source => [source.id, source])), [sources]);
  const verifiedClaims = claims.filter(claim => claim.verificationStatus === 'verified');
  const pendingClaims = claims.filter(claim => claim.verificationStatus !== 'verified');

  const markdown = useMemo(() => {
    const title = projectTitle || 'Research brief';
    const lines = [`# Research brief: ${title}`, '', '## Verified claims'];
    if (verifiedClaims.length === 0) lines.push('No verified claims are ready for production.');
    for (const claim of verifiedClaims) {
      lines.push(`- ${claim.text}`);
      for (const evidenceId of claim.evidenceIds) {
        const item = evidenceById.get(evidenceId);
        const source = item ? sourceById.get(item.sourceId) : undefined;
        if (item) lines.push(`  - Evidence: “${item.quote}” — ${source?.title ?? 'Source'}${item.locator ? ` (${item.locator})` : ''}`);
      }
    }
    lines.push('', '## Sources');
    if (sources.length === 0) lines.push('No sources recorded.');
    for (const source of sources) lines.push(`- ${source.title}: ${source.url}`);
    lines.push('', `## Review status`, `- ${pendingClaims.length} claim${pendingClaims.length === 1 ? '' : 's'} still require review.`);
    return lines.join('\n');
  }, [evidenceById, pendingClaims.length, projectTitle, sourceById, sources, verifiedClaims]);

  async function copyBrief() {
    try {
      await navigator.clipboard.writeText(savedBrief?.markdown || markdown);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1800);
    } catch {
      setError('Clipboard access is unavailable. Copy the brief directly from the preview.');
    }
  }

  async function saveBrief() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId || !researchProjectId) return;
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/brief`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: scopedWorkspaceId })
      });
      if (!response.ok) throw new Error(await response.text());
      const brief = await response.json() as SavedBrief;
      setSavedBrief(brief);
      onSaved?.();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to save the research brief.');
    } finally {
      setBusy(false);
    }
  }

  return <section className="mt-5 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-5">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">Content brief</p><h3 className="mt-1 text-lg font-semibold">Production-ready research summary</h3>{savedBrief && <p className="mt-1 text-xs text-zinc-500">{savedBrief.status === 'published' ? 'Saved as a production brief.' : 'Saved as a draft while claims still need review.'}</p>}</div><div className="flex flex-wrap gap-2"><button onClick={() => void copyBrief()} className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-700 px-3 py-2 text-xs text-zinc-200 hover:bg-zinc-900"><Clipboard size={14} />{copied ? 'Copied' : 'Copy brief'}</button><button onClick={() => void saveBrief()} disabled={busy} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><Save size={14} />{busy ? 'Saving...' : 'Save brief'}</button></div></div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    <div className="mt-4 grid gap-3 sm:grid-cols-3"><div className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-3"><CheckCircle2 size={16} className="text-zinc-300" /><p className="mt-2 text-xl font-semibold">{verifiedClaims.length}</p><p className="text-xs text-zinc-600">verified claims</p></div><div className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-3"><FileText size={16} className="text-zinc-300" /><p className="mt-2 text-xl font-semibold">{sources.length}</p><p className="text-xs text-zinc-600">recorded sources</p></div><div className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-3"><ShieldAlert size={16} className="text-zinc-500" /><p className="mt-2 text-xl font-semibold">{pendingClaims.length}</p><p className="text-xs text-zinc-600">claims to review</p></div></div>
    <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_.8fr]"><div><h4 className="text-sm font-medium">Verified claims</h4>{verifiedClaims.length === 0 ? <p className="mt-3 rounded-xl border border-dashed border-zinc-800 p-4 text-sm text-zinc-600">Verify at least one claim before using this brief in production.</p> : <div className="mt-3 space-y-3">{verifiedClaims.map(claim => <article key={claim.id} className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-4"><p className="text-sm leading-6 text-zinc-200">{claim.text}</p>{claim.evidenceIds.map(evidenceId => { const item = evidenceById.get(evidenceId); const source = item ? sourceById.get(item.sourceId) : undefined; return item ? <p key={evidenceId} className="mt-3 border-l border-zinc-700 pl-3 text-xs leading-5 text-zinc-500">“{item.quote}”<span className="mt-1 block text-zinc-600">{source?.title ?? 'Source'}{item.locator ? ` · ${item.locator}` : ''}</span></p> : null; })}</article>)}</div>}</div><div><h4 className="text-sm font-medium">Source inventory</h4><div className="mt-3 space-y-2">{sources.map(source => <a key={source.id} href={source.url} target="_blank" rel="noreferrer" className="block rounded-xl border border-zinc-800 px-3 py-3 text-xs text-zinc-400 hover:bg-zinc-900"><span className="block font-medium text-zinc-200">{source.title}</span><span className="mt-1 block truncate text-zinc-600">{source.url}</span></a>)}{sources.length === 0 && <p className="rounded-xl border border-dashed border-zinc-800 p-4 text-sm text-zinc-600">Add sources to build a traceable brief.</p>}</div></div></div>
    <details className="mt-5"><summary className="cursor-pointer text-xs text-zinc-500 hover:text-zinc-300">View Markdown export</summary><pre className="mt-3 max-h-72 overflow-auto whitespace-pre-wrap rounded-xl border border-zinc-800 bg-zinc-900/50 p-4 font-mono text-xs leading-5 text-zinc-400">{savedBrief?.markdown || markdown}</pre></details>
  </section>;
}
