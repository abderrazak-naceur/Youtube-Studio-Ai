import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { OpportunityPanel } from './OpportunityPanel';

describe('OpportunityPanel', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('loads and renders workspace opportunities with scores and rationale', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => [{
        id: 'op-1',
        title: 'AI creator workflow',
        status: 'New',
        opportunityScore: 92,
        revenueScore: 81,
        audienceProblem: 'Creators waste time planning.',
        rationale: 'High demand with clear monetization.'
      }]
    }));

    render(<OpportunityPanel apiBase="http://api.test" workspaceId="workspace-1" />);

    expect(await screen.findByText('AI creator workflow')).toBeInTheDocument();
    expect(screen.getByText('92')).toBeInTheDocument();
    expect(screen.getByText('81')).toBeInTheDocument();
    expect(screen.getByText(/Creators waste time planning/)).toBeInTheDocument();
    expect(screen.getByText(/High demand with clear monetization/)).toBeInTheDocument();
    expect(fetch).toHaveBeenCalledWith(
      'http://api.test/api/v1/opportunities?workspaceId=workspace-1&sort=opportunityScore&direction=desc'
    );
  });

  it('prevents saving invalid scores before making an API request', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => [] });
    vi.stubGlobal('fetch', fetchMock);

    render(<OpportunityPanel apiBase="http://api.test" workspaceId="workspace-1" />);
    fireEvent.click(screen.getByRole('button', { name: /Add opportunity/i }));
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Invalid score example' } });
    fireEvent.change(screen.getByLabelText('Opportunity score'), { target: { value: '101' } });
    fireEvent.click(screen.getByRole('button', { name: /Save opportunity/i }));

    await waitFor(() => expect(screen.getByText('Scores must be between 0 and 100.')).toBeInTheDocument());
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
