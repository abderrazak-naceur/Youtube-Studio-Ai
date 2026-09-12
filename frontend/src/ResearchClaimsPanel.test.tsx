import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ResearchClaimsPanel } from './ResearchClaimsPanel';

describe('ResearchClaimsPanel', () => {
  it('loads workspace-scoped claims and evidence', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'claim-1', text: 'A supported fact', verificationStatus: 'unverified', evidenceIds: ['evidence-1'] }] })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'evidence-1', quote: 'Exact supporting quote', locator: 'p. 4' }] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchClaimsPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" sources={[{ id: 'source-1', title: 'Official report' }]} />);

    expect(await screen.findByText('A supported fact')).toBeInTheDocument();
    expect(screen.getByText('Exact supporting quote')).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenNthCalledWith(1, 'http://api.test/api/v1/research-projects/project-1/claims?workspaceId=workspace-1'));
    await waitFor(() => expect(fetchMock).toHaveBeenNthCalledWith(2, 'http://api.test/api/v1/research-projects/project-1/sources/source-1/evidence?workspaceId=workspace-1'));
  });

  it('creates a claim with selected evidence and default unverified status', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [] })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'evidence-1', quote: 'Supporting quote', locator: 'p. 2' }] })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ id: 'claim-1', text: 'New claim', verificationStatus: 'unverified', evidenceIds: ['evidence-1'] }) })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'claim-1', text: 'New claim', verificationStatus: 'unverified', evidenceIds: ['evidence-1'] }] })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'evidence-1', quote: 'Supporting quote', locator: 'p. 2' }] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchClaimsPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" sources={[{ id: 'source-1', title: 'Report' }]} />);

    await screen.findByText('Supporting quote');
    fireEvent.change(screen.getByLabelText('Claim'), { target: { value: 'New claim' } });
    fireEvent.click(screen.getByRole('checkbox'));
    fireEvent.click(screen.getByRole('button', { name: /Add claim/i }));

    const expectedCreate = {
      method: 'POST',
      body: JSON.stringify({ workspaceId: 'workspace-1', text: 'New claim', evidenceIds: ['evidence-1'], verificationStatus: 'unverified' })
    };
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('http://api.test/api/v1/research-projects/project-1/claims', expectedCreate));
  });

  it('updates claim verification with workspace scope and refreshes the claim state', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'claim-1', text: 'Claim', verificationStatus: 'unverified', evidenceIds: [] }] })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ id: 'claim-1', text: 'Claim', verificationStatus: 'verified', evidenceIds: [] }) })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'claim-1', text: 'Claim', verificationStatus: 'verified', evidenceIds: [] }] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchClaimsPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" sources={[]} />);

    expect(await screen.findByText('Claim')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Mark verified' }));

    const expectedVerification = {
      method: 'PUT',
      body: JSON.stringify({ workspaceId: 'workspace-1', verificationStatus: 'verified' })
    };
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('http://api.test/api/v1/research-projects/project-1/claims/claim-1/verification', expectedVerification));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
  });
});
