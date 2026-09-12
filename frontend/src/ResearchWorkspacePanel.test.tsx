import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ResearchWorkspacePanel } from './ResearchWorkspacePanel';

describe('ResearchWorkspacePanel', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('loads a workspace research project and its sources', async () => {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/research-projects?')) {
        return Promise.resolve({ ok: true, json: async () => [{ id: 'research-1', opportunityId: 'opportunity-1', status: 'draft' }] });
      }
      if (url.includes('/opportunities?')) {
        return Promise.resolve({ ok: true, json: async () => [{ id: 'opportunity-1', title: 'AI creator workflow' }] });
      }
      if (url.includes('/claims?')) {
        return Promise.resolve({ ok: true, json: async () => [] });
      }
      if (url.includes('/evidence?')) {
        return Promise.resolve({ ok: true, json: async () => [] });
      }
      return Promise.resolve({ ok: true, json: async () => [{ id: 'source-1', title: 'Primary research', url: 'https://example.com/research', metadataJson: '{}' }] });
    }));

    render(<ResearchWorkspacePanel apiBase="http://api.test" workspaceId="workspace-1" />);

    expect(await screen.findByRole('option', { name: /AI creator workflow.*draft/i })).toBeInTheDocument();
    expect(await screen.findByText('Primary research')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /https:\/\/example\.com\/research/i })).toHaveAttribute('href', 'https://example.com/research');
  });
});
