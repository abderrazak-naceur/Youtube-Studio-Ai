import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ResearchBriefPanel } from './ResearchBriefPanel';

describe('ResearchBriefPanel', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('creates a traceable brief from verified claims and source evidence', async () => {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/claims?')) return Promise.resolve({ ok: true, json: async () => [
        { id: 'claim-1', text: 'Supported fact', verificationStatus: 'verified', evidenceIds: ['evidence-1'] },
        { id: 'claim-2', text: 'Needs review', verificationStatus: 'unverified', evidenceIds: [] }
      ] });
      if (url.includes('/brief')) return Promise.resolve({ ok: false, json: async () => ({}) });
      return Promise.resolve({ ok: true, json: async () => [{ id: 'evidence-1', quote: 'A source-backed excerpt.', locator: 'p. 4' }] });
    }));

    render(<ResearchBriefPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" projectTitle="Creator planning" sources={[{ id: 'source-1', title: 'Official report', url: 'https://example.com/report' }]} />);

    expect(await screen.findByText('Supported fact')).toBeInTheDocument();
    expect(await screen.findByText('A source-backed excerpt.')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Official report/i })).toHaveAttribute('href', 'https://example.com/report');
  });

  it('saves a production brief for the selected workspace and project', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/brief')) {
        return Promise.resolve({ ok: true, json: async () => ({ markdown: '# Saved brief', status: 'published', pendingClaimCount: 0 }) });
      }
      if (url.includes('/claims?')) return Promise.resolve({ ok: true, json: async () => [{ id: 'claim-1', text: 'Supported fact', verificationStatus: 'verified', evidenceIds: ['evidence-1'] }] });
      if (url.includes('/brief')) return Promise.resolve({ ok: false, json: async () => ({}) });
      return Promise.resolve({ ok: true, json: async () => [{ id: 'evidence-1', quote: 'A source-backed excerpt.', locator: 'p. 4' }] });
    });
    vi.stubGlobal('fetch', fetchMock);
    const onSaved = vi.fn();

    render(<ResearchBriefPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" projectTitle="Creator planning" sources={[{ id: 'source-1', title: 'Official report', url: 'https://example.com/report' }]} onSaved={onSaved} />);

    expect(await screen.findByText('Supported fact')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Save brief/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/brief' &&
      options?.method === 'POST' &&
      options?.body === JSON.stringify({ workspaceId: 'workspace-1' })
    )).toBe(true));
    expect(await screen.findByText('Saved as a production brief.')).toBeInTheDocument();
    expect(onSaved).toHaveBeenCalled();
  });
});
