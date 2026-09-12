import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ResearchSourcesPanel } from './ResearchSourcesPanel';

describe('ResearchSourcesPanel', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('loads sources with the workspace scope', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'project-1', opportunityId: 'op-1', workspaceId: 'workspace-1', status: 'draft' }] })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'source-1', url: 'https://example.com', title: 'Example source' }] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchSourcesPanel apiBase="http://api.test" workspaceId="workspace-1" />);

    expect(await screen.findByText('Example source')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenNthCalledWith(1, 'http://api.test/api/v1/research-projects?workspaceId=workspace-1');
    expect(fetchMock).toHaveBeenNthCalledWith(2, 'http://api.test/api/v1/research-projects/project-1/sources?workspaceId=workspace-1');
  });

  it('sends the project and workspace identifiers when adding a source', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'project-1', opportunityId: 'op-1', workspaceId: 'workspace-1', status: 'draft' }] })
      .mockResolvedValueOnce({ ok: true, json: async () => [] })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ id: 'source-1' }) })
      .mockResolvedValueOnce({ ok: true, json: async () => [{ id: 'source-1', url: 'https://example.com', title: 'Example source' }] });
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchSourcesPanel apiBase="http://api.test" workspaceId="workspace-1" />);
    await screen.findByText('No sources added yet.');
    fireEvent.change(screen.getByPlaceholderText('Source title'), { target: { value: 'Example source' } });
    fireEvent.change(screen.getByPlaceholderText('https://example.com/article'), { target: { value: 'https://example.com' } });
    fireEvent.click(screen.getByRole('button', { name: /Add/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenNthCalledWith(3,
      'http://api.test/api/v1/research-projects/project-1/sources',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ workspaceId: 'workspace-1', researchProjectId: 'project-1', url: 'https://example.com', title: 'Example source', metadataJson: '{}' })
      })
    ));
  });
});
