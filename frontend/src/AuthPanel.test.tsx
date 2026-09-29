import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthPanel } from './AuthPanel';

describe('AuthPanel', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
  });

  it('logs in and reports the token', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/auth/login') && init?.method === 'POST') {
        return Promise.resolve({ ok: true, json: async () => ({ userId: 'u1', email: 'a@b.com', accessToken: 'tok-123', expiresAtUtc: '2030-01-01' }) });
      }
      return Promise.resolve({ ok: false, text: async () => 'nope' });
    });
    vi.stubGlobal('fetch', fetchMock);
    const onAuthenticated = vi.fn();

    render(<AuthPanel apiBase="http://api.test" onAuthenticated={onAuthenticated} />);

    fireEvent.change(screen.getByPlaceholderText('you@example.com'), { target: { value: 'a@b.com' } });
    fireEvent.change(screen.getByPlaceholderText('At least 8 characters'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByRole('button', { name: /Sign in/i }));

    await waitFor(() => expect(onAuthenticated).toHaveBeenCalledWith('tok-123', 'a@b.com'));
    expect(window.localStorage.getItem('ysa.token')).toBe('tok-123');
  });

  it('validates password length before calling the API', async () => {
    const fetchMock = vi.fn(() => Promise.resolve({ ok: true, json: async () => ({}) }));
    vi.stubGlobal('fetch', fetchMock);

    render(<AuthPanel apiBase="http://api.test" onAuthenticated={vi.fn()} />);

    fireEvent.change(screen.getByPlaceholderText('you@example.com'), { target: { value: 'a@b.com' } });
    fireEvent.change(screen.getByPlaceholderText('At least 8 characters'), { target: { value: 'short' } });
    fireEvent.click(screen.getByRole('button', { name: /Sign in/i }));

    expect(await screen.findByText('Password must be at least 8 characters.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('can switch to register mode and posts to the register endpoint', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/auth/register')) {
        return Promise.resolve({ ok: true, json: async () => ({ userId: 'u2', email: 'new@b.com', accessToken: 'tok-9', expiresAtUtc: '2030-01-01' }) });
      }
      return Promise.resolve({ ok: false, text: async () => 'nope' });
    });
    vi.stubGlobal('fetch', fetchMock);
    const onAuthenticated = vi.fn();

    render(<AuthPanel apiBase="http://api.test" onAuthenticated={onAuthenticated} />);

    fireEvent.click(screen.getByRole('button', { name: /Need an account\? Register/i }));
    fireEvent.change(screen.getByPlaceholderText('you@example.com'), { target: { value: 'new@b.com' } });
    fireEvent.change(screen.getByPlaceholderText('At least 8 characters'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByRole('button', { name: /Create account/i }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) => String(url).endsWith('/auth/register'))).toBe(true));
    await waitFor(() => expect(onAuthenticated).toHaveBeenCalledWith('tok-9', 'new@b.com'));
  });
});
