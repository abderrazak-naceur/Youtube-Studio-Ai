import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ResearchEvidencePanel } from './ResearchEvidencePanel';

const source = { id: 'source-1', title: 'Primary source' };

describe('ResearchEvidencePanel', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('loads evidence scoped to the selected workspace and source', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchEvidencePanel apiBase="" workspaceId="workspace-1" researchProjectId="project-1" sources={[source]} />);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/research-projects/project-1/sources/source-1/evidence?workspaceId=workspace-1'));
  });

  it('posts the selected source, quote and provenance fields', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [] })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ id: 'evidence-1' }) })
      .mockResolvedValueOnce({ ok: true, json: async () => [] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchEvidencePanel apiBase="" workspaceId="workspace-1" researchProjectId="project-1" sources={[source]} />);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/research-projects/project-1/sources/source-1/evidence?workspaceId=workspace-1'));

    fireEvent.change(screen.getByLabelText('Evidence quote'), { target: { value: 'Exact quote' } });
    fireEvent.change(screen.getByLabelText('Evidence locator'), { target: { value: 'p. 10' } });
    fireEvent.click(screen.getByRole('button', { name: /Add evidence/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/research-projects/project-1/sources/source-1/evidence', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ workspaceId: 'workspace-1', researchSourceId: 'source-1', quote: 'Exact quote', locator: 'p. 10', context: null, metadataJson: null })
    }));
  });
});
