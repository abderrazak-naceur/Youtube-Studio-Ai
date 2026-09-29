import { useState } from 'react';
import { LogIn, UserPlus } from 'lucide-react';

type AuthResponse = { userId: string; email: string; accessToken: string; expiresAtUtc: string };

type Props = {
  apiBase: string;
  onAuthenticated: (token: string, email: string) => void;
};

export function AuthPanel({ apiBase, onAuthenticated }: Props) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit() {
    const trimmedEmail = email.trim();
    if (!trimmedEmail) return setError('Enter your email.');
    if (password.length < 8) return setError('Password must be at least 8 characters.');
    setBusy(true);
    setError('');
    try {
      const body = mode === 'register'
        ? { email: trimmedEmail, password, displayName: displayName.trim() || null }
        : { email: trimmedEmail, password };
      const response = await fetch(`${apiBase}/api/v1/auth/${mode}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (!response.ok) {
        throw new Error(mode === 'login' ? 'Invalid email or password.' : await response.text());
      }
      const payload = await response.json() as AuthResponse;
      try { window.localStorage.setItem('ysa.token', payload.accessToken); } catch { /* storage optional */ }
      onAuthenticated(payload.accessToken, payload.email);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Authentication failed.');
    } finally {
      setBusy(false);
    }
  }

  return <section className="mx-auto mt-16 w-full max-w-sm rounded-3xl border border-zinc-800 bg-zinc-950/60 p-6">
    <h2 className="text-lg font-semibold">{mode === 'login' ? 'Sign in' : 'Create account'}</h2>
    <p className="mt-1 text-xs text-zinc-500">Access your Youtube Studio AI workspaces.</p>

    <div className="mt-5 grid gap-3">
      <label className="text-xs text-zinc-500">Email<input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@example.com" className="mt-1 w-full rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-sm text-zinc-200 outline-none focus:border-zinc-600" /></label>
      {mode === 'register' && <label className="text-xs text-zinc-500">Display name<input value={displayName} onChange={e => setDisplayName(e.target.value)} placeholder="Optional" className="mt-1 w-full rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-sm text-zinc-200 outline-none focus:border-zinc-600" /></label>}
      <label className="text-xs text-zinc-500">Password<input type="password" value={password} onChange={e => setPassword(e.target.value)} placeholder="At least 8 characters" className="mt-1 w-full rounded-lg border border-zinc-800 bg-zinc-950 px-3 py-2 text-sm text-zinc-200 outline-none focus:border-zinc-600" /></label>
    </div>

    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}

    <button onClick={() => void submit()} disabled={busy} className="mt-4 inline-flex w-full items-center justify-center gap-2 rounded-lg bg-white px-4 py-2.5 text-sm font-semibold text-zinc-950 disabled:opacity-40">
      {mode === 'login' ? <LogIn size={15} /> : <UserPlus size={15} />}
      {busy ? 'Working...' : mode === 'login' ? 'Sign in' : 'Create account'}
    </button>

    <button onClick={() => { setError(''); setMode(mode === 'login' ? 'register' : 'login'); }} className="mt-3 w-full text-center text-xs text-zinc-500 hover:text-zinc-300">
      {mode === 'login' ? 'Need an account? Register' : 'Have an account? Sign in'}
    </button>
  </section>;
}
