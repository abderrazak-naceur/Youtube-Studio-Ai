import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FactCheckPanel } from './FactCheckPanel';

const passedReport = {
  verdict: 'passed',
  claimCount: 1,
  verifiedClaimCount: 1,
  disputedClaimCount: 0,
  unverifiedClaimCount: 0,
  unsupportedClaimCount: 0,
  requiresHumanReview: false,
  findings: [{ id: 'finding-1', researchClaimId: 'claim-1', status: 'supported', riskLevel: 'low', evidenceCount: 2, requiresHumanReview: false, rationale: 'Verified claim backed by 2 evidence records.' }]
};

describe('FactCheckPanel', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('loads an existing fact check report for the workspace and project', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/fact-check?')) return Promise.resolve({ ok: true, json: async () => passedReport });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<FactCheckPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" claims={[{ id: 'claim-1', text: 'A supported fact' }]} />);

    expect(await screen.findByText('A supported fact')).toBeInTheDocument();
    expect(screen.getByLabelText('Verdict: Passed')).toBeInTheDocument();
    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/fact-check?workspaceId=workspace-1')).toBe(true));
  });

  it('runs a fact check for the selected workspace and project', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/fact-check')) return Promise.resolve({ ok: true, json: async () => passedReport });
      if (url.includes('/fact-check?')) return Promise.resolve({ ok: false, json: async () => ({}) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);
    const onChecked = vi.fn();

    render(<FactCheckPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" claims={[{ id: 'claim-1', text: 'A supported fact' }]} onChecked={onChecked} />);

    await screen.findByText('No fact check yet. Run a check once you have recorded claims.');
    fireEvent.click(screen.getByRole('button', { name: /Run fact check/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/fact-check' &&
      options?.method === 'POST' &&
      (options?.headers as Record<string, string>)?.['Content-Type'] === 'application/json' &&
      options?.body === JSON.stringify({ workspaceId: 'workspace-1' })
    )).toBe(true));
    expect(await screen.findByLabelText('Verdict: Passed')).toBeInTheDocument();
    expect(onChecked).toHaveBeenCalled();
  });

  it('surfaces the human review flag for high-risk findings', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/fact-check?')) return Promise.resolve({ ok: true, json: async () => ({
        ...passedReport,
        verdict: 'needs_review',
        requiresHumanReview: true,
        findings: [{ id: 'finding-1', researchClaimId: 'claim-1', status: 'supported', riskLevel: 'high', evidenceCount: 1, requiresHumanReview: true, rationale: 'High-risk domain.' }]
      }) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<FactCheckPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" claims={[{ id: 'claim-1', text: 'Investment claim' }]} />);

    expect(await screen.findByText('Human review required')).toBeInTheDocument();
    expect(screen.getByLabelText('Verdict: Needs review')).toBeInTheDocument();
    expect(screen.getByText('high risk')).toBeInTheDocument();
  });
});
