import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AiProviderStatusPanel } from './AiProviderStatusPanel';

describe('AiProviderStatusPanel', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('loads and displays the model routing table', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL) => ({
      ok: true,
      json: async () => ({
        routes: [
          { task: 'Research', provider: 'placeholder', isFallback: true },
          { task: 'Voice', provider: 'elevenlabs', isFallback: false }
        ]
      })
    }));
    vi.stubGlobal('fetch', fetchMock);

    render(<AiProviderStatusPanel apiBase="http://api.test" />);

    expect(await screen.findByText('Research')).toBeInTheDocument();
    expect(screen.getByText('elevenlabs')).toBeInTheDocument();
    expect(screen.getByText('configured')).toBeInTheDocument();
    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) =>
      String(url) === 'http://api.test/api/v1/ai-providers')).toBe(true));
  });

  it('surfaces an error when routing cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => ({ ok: false, text: async () => 'boom' })));

    render(<AiProviderStatusPanel apiBase="http://api.test" />);

    expect(await screen.findByText('boom')).toBeInTheDocument();
  });
});
