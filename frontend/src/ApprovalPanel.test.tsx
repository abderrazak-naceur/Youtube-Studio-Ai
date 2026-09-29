import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApprovalPanel } from './ApprovalPanel';

describe('ApprovalPanel', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('shows the estimated production cost from the costs endpoint', async () => {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/costs')) return Promise.resolve({ ok: true, json: async () => ({
        totalCostUsd: 0.0968,
        byStage: [{ stage: 'Render', totalCostUsd: 0.0168 }, { stage: 'Visual', totalCostUsd: 0.04 }]
      }) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    }));

    render(<ApprovalPanel apiBase="http://api.test" videoProjectId="project-1" />);

    expect(await screen.findByText('$0.0968')).toBeInTheDocument();
    expect(screen.getByText('Render')).toBeInTheDocument();
  });

  it('approves the project with the reviewer name and notifies the parent', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/approve')) {
        return Promise.resolve({ ok: true, json: async () => ({ status: 'Completed', decision: 'approved' }) });
      }
      if (url.includes('/costs')) return Promise.resolve({ ok: true, json: async () => ({ totalCostUsd: 0.05, byStage: [] }) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);
    const onDecided = vi.fn();

    render(<ApprovalPanel apiBase="http://api.test" videoProjectId="project-1" onDecided={onDecided} />);

    fireEvent.change(screen.getByPlaceholderText('Your name'), { target: { value: 'Alex' } });
    fireEvent.click(screen.getByRole('checkbox'));
    fireEvent.click(screen.getByRole('button', { name: /Approve & export/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/video-projects/project-1/approve' &&
      options?.method === 'POST' &&
      options?.body === JSON.stringify({ reviewer: 'Alex', notes: null, aiDisclosureAcknowledged: true })
    )).toBe(true));
    await waitFor(() => expect(onDecided).toHaveBeenCalledWith('Completed'));
  });

  it('requires a reviewer before approving', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/costs')) return Promise.resolve({ ok: true, json: async () => ({ totalCostUsd: 0, byStage: [] }) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ApprovalPanel apiBase="http://api.test" videoProjectId="project-1" />);

    fireEvent.click(screen.getByRole('button', { name: /Approve & export/i }));

    expect(await screen.findByText('Enter a reviewer name before deciding.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).endsWith('/approve'))).toBe(false);
  });

  it('requires the AI disclosure to be acknowledged before approving', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/costs')) return Promise.resolve({ ok: true, json: async () => ({ totalCostUsd: 0, byStage: [] }) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ApprovalPanel apiBase="http://api.test" videoProjectId="project-1" />);

    fireEvent.change(screen.getByPlaceholderText('Your name'), { target: { value: 'Alex' } });
    fireEvent.click(screen.getByRole('button', { name: /Approve & export/i }));

    expect(await screen.findByText('Acknowledge the AI disclosure before approving.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).endsWith('/approve'))).toBe(false);
  });

  it('requires notes before rejecting', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/costs')) return Promise.resolve({ ok: true, json: async () => ({ totalCostUsd: 0, byStage: [] }) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ApprovalPanel apiBase="http://api.test" videoProjectId="project-1" />);

    fireEvent.change(screen.getByPlaceholderText('Your name'), { target: { value: 'Alex' } });
    fireEvent.click(screen.getByRole('button', { name: /Reject/i }));

    expect(await screen.findByText('Rejection notes are required.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).endsWith('/reject'))).toBe(false);
  });
});
