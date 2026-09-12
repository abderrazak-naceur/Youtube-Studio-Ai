import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ResearchClaimsPanel } from './ResearchClaimsPanel';

describe('ResearchClaimsPanel', () => {
  it('loads workspace-scoped claims and evidence', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/claims?')) {
        return { ok: true, json: async () => [{ id: 'claim-1', text: 'A supported fact', verificationStatus: 'unverified', evidenceIds: ['evidence-1'] }] };
      }
      return { ok: true, json: async () => [{ id: 'evidence-1', quote: 'Exact supporting quote', locator: 'p. 4' }] };
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchClaimsPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" sources={[{ id: 'source-1', title: 'Official report' }]} />);

    expect(await screen.findByText('A supported fact')).toBeInTheDocument();
    expect(screen.getByText('Exact supporting quote', { selector: 'p' })).toBeInTheDocument();
    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) => String(url) === 'http://api.test/api/v1/research-projects/project-1/claims?workspaceId=workspace-1')).toBe(true));
    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) => String(url) === 'http://api.test/api/v1/research-projects/project-1/sources/source-1/evidence?workspaceId=workspace-1')).toBe(true));
  });

  it('creates a claim with selected evidence and default unverified status', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/claims')) {
        return { ok: true, json: async () => ({ id: 'claim-1', text: 'New claim', verificationStatus: 'unverified', evidenceIds: ['evidence-1'] }) };
      }
      if (url.includes('/claims?')) {
        return { ok: true, json: async () => [] };
      }
      return { ok: true, json: async () => [{ id: 'evidence-1', quote: 'Supporting quote', locator: 'p. 2' }] };
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchClaimsPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" sources={[{ id: 'source-1', title: 'Report' }]} />);

    await screen.findByText('Supporting quote');
    fireEvent.change(screen.getByLabelText('Claim'), { target: { value: 'New claim' } });
    fireEvent.click(screen.getByRole('checkbox'));
    fireEvent.click(screen.getByRole('button', { name: /Add claim/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/claims' &&
      options?.method === 'POST' &&
      (options?.headers as Record<string, string>)?.['Content-Type'] === 'application/json' &&
      options?.body === JSON.stringify({ workspaceId: 'workspace-1', text: 'New claim', evidenceIds: ['evidence-1'], verificationStatus: 'unverified' })
    )).toBe(true));
  });

  it('updates claim verification with workspace scope and refreshes the claim state', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'PUT' && url.endsWith('/verification')) {
        return { ok: true, json: async () => ({ id: 'claim-1', text: 'Claim', verificationStatus: 'verified', evidenceIds: [] }) };
      }
      return { ok: true, json: async () => [{ id: 'claim-1', text: 'Claim', verificationStatus: 'unverified', evidenceIds: [] }] };
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchClaimsPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" sources={[]} />);

    expect(await screen.findByText('Claim')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Mark verified' }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/claims/claim-1/verification' &&
      options?.method === 'PUT' &&
      (options?.headers as Record<string, string>)?.['Content-Type'] === 'application/json' &&
      options?.body === JSON.stringify({ workspaceId: 'workspace-1', verificationStatus: 'verified' })
    )).toBe(true));
    await waitFor(() => expect(fetchMock.mock.calls.filter(([url]) => String(url).includes('/claims?')).length).toBeGreaterThanOrEqual(2));
  });
});
