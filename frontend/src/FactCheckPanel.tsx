import { useEffect, useState } from 'react';
import { AlertTriangle, CheckCircle2, CircleAlert, ShieldCheck, ShieldQuestion } from 'lucide-react';

type Finding = {
  id: string;
  researchClaimId: string;
  status: string;
  riskLevel: string;
  evidenceCount: number;
  requiresHumanReview: boolean;
  rationale: string;
};
type Report = {
  verdict: string;
  claimCount: number;
  verifiedClaimCount: number;
  disputedClaimCount: number;
  unverifiedClaimCount: number;
  unsupportedClaimCount: number;
  requiresHumanReview: boolean;
  findings: Finding[];
};
type Claim = { id: string; text: string };
type Props = { apiBase: string; workspaceId: string; researchProjectId: string; claims?: Claim[]; onChecked?: () => void };

const verdictLabels: Record<string, string> = {
  passed: 'Passed',
  needs_review: 'Needs review',
  failed: 'Failed'
};

function normalizeReport(payload: Partial<Report>): Report {
  return {
    verdict: typeof payload.verdict === 'string' ? payload.verdict : 'needs_review',
    claimCount: payload.claimCount ?? 0,
    verifiedClaimCount: payload.verifiedClaimCount ?? 0,
    disputedClaimCount: payload.disputedClaimCount ?? 0,
    unverifiedClaimCount: payload.unverifiedClaimCount ?? 0,
    unsupportedClaimCount: payload.unsupportedClaimCount ?? 0,
    requiresHumanReview: Boolean(payload.requiresHumanReview),
    findings: Array.isArray(payload.findings) ? payload.findings : []
  };
}

export function FactCheckPanel({ apiBase, workspaceId, researchProjectId, claims = [], onChecked }: Props) {
  const [report, setReport] = useState<Report | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId || !researchProjectId) { setReport(null); return; }
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/fact-check?workspaceId=${encodeURIComponent(scopedWorkspaceId)}`);
      if (response.ok) setReport(normalizeReport(await response.json()));
      else setReport(null);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to load the fact check.');
    }
  }

  useEffect(() => { void load(); }, [workspaceId, researchProjectId]);

  async function runFactCheck() {
    const scopedWorkspaceId = workspaceId.trim();
    if (!scopedWorkspaceId || !researchProjectId) return;
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/research-projects/${researchProjectId}/fact-check`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workspaceId: scopedWorkspaceId })
      });
      if (!response.ok) throw new Error(await response.text());
      setReport(normalizeReport(await response.json()));
      onChecked?.();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to run the fact check.');
    } finally {
      setBusy(false);
    }
  }

  const claimTextById = new Map(claims.map(claim => [claim.id, claim.text]));
  const verdict = report?.verdict ?? 'needs_review';
  const VerdictIcon = verdict === 'passed' ? ShieldCheck : verdict === 'failed' ? CircleAlert : ShieldQuestion;

  return <section className="mt-5 rounded-2xl border border-zinc-800 bg-zinc-950/60 p-5">
    <div className="flex flex-wrap items-start justify-between gap-3">
      <div>
        <p className="text-xs font-semibold uppercase tracking-wider text-zinc-600">Compliance gate</p>
        <h3 className="mt-1 text-lg font-semibold">Fact check before publication</h3>
        <p className="mt-1 text-xs text-zinc-500">Evaluate every claim for evidence, disputes and high-risk domains.</p>
      </div>
      <button onClick={() => void runFactCheck()} disabled={busy || !researchProjectId} className="inline-flex items-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-zinc-950 disabled:opacity-40"><ShieldCheck size={14} />{busy ? 'Checking...' : 'Run fact check'}</button>
    </div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    {!report ? <p className="mt-4 rounded-xl border border-dashed border-zinc-800 p-8 text-center text-sm text-zinc-600">No fact check yet. Run a check once you have recorded claims.</p> : <>
      <div className="mt-4 flex flex-wrap items-center gap-3">
        <span aria-label={`Verdict: ${verdictLabels[verdict] ?? verdict}`} className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-700 px-3 py-1.5 text-sm font-semibold"><VerdictIcon size={16} />{verdictLabels[verdict] ?? verdict}</span>
        {report.requiresHumanReview && <span className="inline-flex items-center gap-1.5 rounded-lg border border-amber-800/60 bg-amber-950/20 px-3 py-1.5 text-xs text-amber-300"><AlertTriangle size={14} /> Human review required</span>}
      </div>
      <div className="mt-4 grid gap-3 sm:grid-cols-5">
        <Stat label="claims" value={report.claimCount} />
        <Stat label="verified" value={report.verifiedClaimCount} />
        <Stat label="unverified" value={report.unverifiedClaimCount} />
        <Stat label="disputed" value={report.disputedClaimCount} />
        <Stat label="unsupported" value={report.unsupportedClaimCount} />
      </div>
      <div className="mt-5 space-y-2">{report.findings.map(finding => <article key={finding.id} className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-4">
        <div className="flex items-start justify-between gap-3">
          <p className="text-sm leading-6 text-zinc-200">{claimTextById.get(finding.researchClaimId) ?? 'Claim'}</p>
          {finding.status === 'supported' ? <CheckCircle2 size={17} className="shrink-0" /> : <CircleAlert size={17} className="shrink-0 text-zinc-500" />}
        </div>
        <p className="mt-2 text-xs text-zinc-500">{finding.rationale}</p>
        <div className="mt-3 flex flex-wrap items-center gap-2 text-[11px]">
          <span className="rounded-md border border-zinc-700 px-2 py-1 text-zinc-400">{finding.status}</span>
          <span className={`rounded-md border px-2 py-1 ${finding.riskLevel === 'high' ? 'border-amber-800/60 text-amber-300' : 'border-zinc-700 text-zinc-500'}`}>{finding.riskLevel} risk</span>
          <span className="rounded-md border border-zinc-700 px-2 py-1 text-zinc-500">{finding.evidenceCount} evidence</span>
          {finding.requiresHumanReview && <span className="rounded-md border border-amber-800/60 px-2 py-1 text-amber-300">review</span>}
        </div>
      </article>)}</div>
    </>}
  </section>;
}

function Stat({ label, value }: { label: string; value: number }) {
  return <div className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-3"><p className="text-xl font-semibold">{value}</p><p className="text-xs text-zinc-600">{label}</p></div>;
}
