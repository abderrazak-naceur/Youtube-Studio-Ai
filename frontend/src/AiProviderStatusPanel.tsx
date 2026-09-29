import { useEffect, useState } from 'react';
import { Cpu, RefreshCw } from 'lucide-react';

type Route = { task: string; provider: string; isFallback: boolean };
type Props = { apiBase: string };

export function AiProviderStatusPanel({ apiBase }: Props) {
  const [routes, setRoutes] = useState<Route[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiBase}/api/v1/ai-providers`);
      if (!response.ok) throw new Error(await response.text());
      const payload = await response.json();
      setRoutes(Array.isArray(payload?.routes) ? payload.routes : []);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to load AI provider routing.');
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => { void load(); }, [apiBase]);

  return <section className="mt-6 rounded-3xl border border-zinc-800 bg-zinc-900/40 p-6">
    <div className="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">AI provider layer</p>
        <h2 className="mt-1 text-xl font-semibold">Model routing</h2>
        <p className="mt-1 text-xs text-zinc-500">Which provider each production task is routed to. Configure selections in <code className="text-zinc-400">AiProviders</code>.</p>
      </div>
      <button onClick={() => void load()} disabled={busy} className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-700 px-3 py-2 text-xs text-zinc-200 hover:bg-zinc-900 disabled:opacity-40"><RefreshCw size={14} className={busy ? 'animate-spin' : ''} /> Refresh</button>
    </div>
    {error && <p className="mt-3 rounded-lg border border-red-900/50 bg-red-950/20 px-3 py-2 text-xs text-red-300">{error}</p>}
    {routes.length === 0 && !error ? <p className="mt-4 text-sm text-zinc-600">No routing information available.</p> : <div className="mt-5 grid gap-2 sm:grid-cols-2 xl:grid-cols-3">{routes.map(route => <article key={route.task} className="flex items-center justify-between gap-3 rounded-xl border border-zinc-800 bg-zinc-950/60 p-3">
      <div className="flex items-center gap-2"><Cpu size={15} className="text-zinc-500" /><span className="text-sm text-zinc-200">{route.task}</span></div>
      <div className="text-right"><span className="text-sm font-medium text-zinc-300">{route.provider}</span><span className="block text-[11px] text-zinc-600">{route.isFallback ? 'default' : 'configured'}</span></div>
    </article>)}</div>}
  </section>;
}
