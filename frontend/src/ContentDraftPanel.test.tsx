import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentDraftPanel } from './ContentDraftPanel';

const readyDraft = {
  id: 'draft-1',
  angle: 'A clear, evidence-based explanation of Creator planning.',
  hook: 'What most people get wrong about Creator planning.',
  outline: '1. Hook\n2. Why this matters',
  script: 'Here is what the evidence really says.',
  titleCandidates: ['The Truth About Creator planning'],
  thumbnailConcepts: ['Bold text over a surprised face.'],
  description: 'In this video we break down Creator planning.',
  chapters: [{ timestamp: '0:00', title: 'Intro' }],
  tags: ['creator planning', 'research'],
  status: 'ready'
};

describe('ContentDraftPanel', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('loads an existing content draft for the workspace and project', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/content-draft?')) return Promise.resolve({ ok: true, json: async () => readyDraft });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ContentDraftPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" />);

    expect(await screen.findByText('The Truth About Creator planning')).toBeInTheDocument();
    expect(screen.getByText('Ready for production')).toBeInTheDocument();
    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/content-draft?workspaceId=workspace-1')).toBe(true));
  });

  it('generates content for the selected workspace and project', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/content-draft')) return Promise.resolve({ ok: true, json: async () => readyDraft });
      if (url.includes('/content-draft?')) return Promise.resolve({ ok: false, json: async () => ({}) });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);
    const onGenerated = vi.fn();

    render(<ContentDraftPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" onGenerated={onGenerated} />);

    await screen.findByText('No content yet. Generate an editorial draft once the fact check is complete.');
    fireEvent.click(screen.getByRole('button', { name: /Generate content/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/research-projects/project-1/content-draft' &&
      options?.method === 'POST' &&
      (options?.headers as Record<string, string>)?.['Content-Type'] === 'application/json' &&
      options?.body === JSON.stringify({ workspaceId: 'workspace-1' })
    )).toBe(true));
    expect(await screen.findByText('The Truth About Creator planning')).toBeInTheDocument();
    expect(onGenerated).toHaveBeenCalled();
  });

  it('surfaces the backend error when generation is blocked by the fact-check gate', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/content-draft')) return Promise.resolve({ ok: false, text: async () => 'Run a fact check before generating content.' });
      return Promise.resolve({ ok: false, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ContentDraftPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" />);

    fireEvent.click(await screen.findByRole('button', { name: /Generate content/i }));

    expect(await screen.findByText('Run a fact check before generating content.')).toBeInTheDocument();
  });

  it('creates a video project from a ready draft', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (init?.method === 'POST' && url.endsWith('/video-projects/from-content-draft')) {
        return Promise.resolve({ ok: true, json: async () => ({ id: 'video-1', source: 'content_draft' }) });
      }
      if (url.includes('/content-draft?')) return Promise.resolve({ ok: true, json: async () => readyDraft });
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ContentDraftPanel apiBase="http://api.test" workspaceId="workspace-1" researchProjectId="project-1" />);

    fireEvent.click(await screen.findByRole('button', { name: /Create video from draft/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url, options]) =>
      String(url) === 'http://api.test/api/v1/video-projects/from-content-draft' &&
      options?.method === 'POST' &&
      options?.body === JSON.stringify({ workspaceId: 'workspace-1', contentDraftId: 'draft-1' })
    )).toBe(true));
    expect(await screen.findByText(/Video project created from this draft/i)).toBeInTheDocument();
  });
});
