import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';

function App() {
  return (
    <main className="min-h-screen p-8">
      <div className="mx-auto max-w-6xl">
        <p className="text-sm font-medium text-zinc-500">YOUTUBE STUDIO AI</p>
        <h1 className="mt-2 text-4xl font-semibold tracking-tight">AI Media Company OS</h1>
        <p className="mt-3 max-w-2xl text-zinc-400">
          Discover opportunities, research topics, produce original videos and learn from performance.
        </p>

        <section className="mt-10 grid gap-4 md:grid-cols-3">
          {[
            ['Opportunities', 'Find ideas with demand, competition and revenue potential.'],
            ['Production', 'Turn a validated opportunity into a complete video pipeline.'],
            ['Analytics', 'Measure results and feed learnings back into the content engine.']
          ].map(([title, description]) => (
            <article key={title} className="rounded-2xl border border-zinc-800 bg-zinc-950 p-6">
              <h2 className="text-lg font-medium">{title}</h2>
              <p className="mt-2 text-sm leading-6 text-zinc-400">{description}</p>
            </article>
          ))}
        </section>
      </div>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
