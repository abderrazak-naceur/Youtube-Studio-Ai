# Technology Stack Decision — Vite Frontend

## Decision

For YouTube Studio AI, the frontend will use **Vite + React + TypeScript + Tailwind CSS** rather than Next.js.

This is an intentional architecture decision because the product is primarily an authenticated SaaS application/workbench whose business logic belongs in the ASP.NET Core backend.

## Why Vite

- very fast local development and HMR
- simple deployment as static assets
- clear separation between frontend and backend
- no need to put business logic into a frontend server
- excellent fit for dashboards, editors, timelines, agent consoles and analytics
- easy Docker/CDN deployment
- low operational complexity

Vite officially supports React + TypeScript templates and provides HMR plus optimized production builds. React's documentation also explicitly provides a Vite path for building React apps from scratch. citeturn0search2turn0search10

## Frontend stack

### Core

- Vite
- React
- TypeScript
- React Router
- Tailwind CSS

### Application libraries

- TanStack Query — server state and API cache
- Zustand — local UI/workflow state
- Axios — HTTP client
- Zod — runtime validation
- React Hook Form — complex forms
- dayjs — dates and scheduling
- Lucide React — icons
- Motion — UI transitions
- React Markdown + remark-gfm — research and AI output rendering
- @microsoft/signalr — real-time job/agent updates
- @xyflow/react — agent/workflow graph visualization
- @monaco-editor/react — advanced script/prompt/config editing

### UI primitives

Use one primary primitive system. **Radix UI** is the default choice for this project. Do not install multiple overlapping component primitive systems unless a specific component requires it.

## Tailwind CSS

Use Tailwind CSS v4 with the first-party Vite plugin `@tailwindcss/vite`. Tailwind's official documentation recommends the dedicated Vite plugin for Vite projects. citeturn0search4turn0search6

## Suggested package baseline

The user's provided package structure is a good starting point. The project should keep dependency versions controlled through the repository lockfile and upgrade deliberately rather than automatically adopting every new major version.

```json
{
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "lint": "eslint .",
    "preview": "vite preview"
  },
  "dependencies": {
    "@hookform/resolvers": "latest-compatible",
    "@microsoft/signalr": "latest-compatible",
    "@monaco-editor/react": "latest-compatible",
    "@tanstack/react-query": "latest-compatible",
    "@xyflow/react": "latest-compatible",
    "axios": "latest-compatible",
    "class-variance-authority": "latest-compatible",
    "clsx": "latest-compatible",
    "dayjs": "latest-compatible",
    "lucide-react": "latest-compatible",
    "motion": "latest-compatible",
    "radix-ui": "latest-compatible",
    "react": "latest-compatible",
    "react-dom": "latest-compatible",
    "react-hook-form": "latest-compatible",
    "react-markdown": "latest-compatible",
    "react-router-dom": "latest-compatible",
    "remark-gfm": "latest-compatible",
    "tailwind-merge": "latest-compatible",
    "tailwindcss": "latest-compatible",
    "zod": "latest-compatible",
    "zustand": "latest-compatible"
  },
  "devDependencies": {
    "@eslint/js": "latest-compatible",
    "@tailwindcss/vite": "latest-compatible",
    "@types/node": "latest-compatible",
    "@types/react": "latest-compatible",
    "@types/react-dom": "latest-compatible",
    "@vitejs/plugin-react": "latest-compatible",
    "eslint": "latest-compatible",
    "eslint-plugin-react-hooks": "latest-compatible",
    "eslint-plugin-react-refresh": "latest-compatible",
    "globals": "latest-compatible",
    "typescript": "latest-compatible",
    "typescript-eslint": "latest-compatible",
    "vite": "latest-compatible"
  }
}
```

`latest-compatible` is a planning placeholder, not a recommendation to use the `latest` tag in production. Commit an exact lockfile and pin production dependencies after the initial scaffold is validated.

## Frontend architecture

```text
apps/web/
├── src/
│   ├── app/
│   ├── routes/
│   ├── components/
│   │   ├── ui/
│   │   ├── layout/
│   │   ├── charts/
│   │   ├── agents/
│   │   └── media/
│   ├── features/
│   │   ├── dashboard/
│   │   ├── opportunities/
│   │   ├── research/
│   │   ├── content/
│   │   ├── production/
│   │   ├── publishing/
│   │   ├── analytics/
│   │   ├── revenue/
│   │   └── channels/
│   ├── api/
│   ├── hooks/
│   ├── stores/
│   ├── schemas/
│   ├── lib/
│   ├── types/
│   └── styles/
├── public/
└── tests/
```

## State rules

### TanStack Query

Use for server state:

- opportunities
- channels
- analytics
- revenue
- research
- jobs
- assets
- content projects

### Zustand

Use only for client state:

- editor panels
- timeline selection
- command palette
- local preferences
- temporary multi-step workflow state

Do not duplicate server state in Zustand.

## Real-time UX

SignalR will provide:

- job progress
- render progress
- AI-agent status
- approval requests
- provider callbacks translated into user-facing events
- publication status

The frontend should subscribe to workspace-scoped channels and invalidate TanStack Query caches when authoritative backend events arrive.

ASP.NET Core SignalR has a first-party JavaScript client package, `@microsoft/signalr`, and supports real-time asynchronous communication. citeturn1search1turn1search5

## Frontend business KPIs

The frontend platform should measure:

- time from opportunity to video draft
- time from draft to approved video
- editor completion rate
- AI action acceptance rate
- retry rate
- generation cancellation rate
- UI error rate
- publish approval time
- cost visibility interaction rate
- mobile approval usage

## Frontend delivery phases

### F1 — Foundation

- Vite scaffold
- React Router
- Tailwind
- design tokens
- AppShell
- authentication screens
- API client
- TanStack Query
- error/loading/empty states
- CI

### F2 — Core workspace

- Dashboard
- Opportunities
- Research
- Channel DNA
- Content projects

### F3 — Creator workstation

- script editor
- scene editor
- asset browser
- voice controls
- timeline
- preview
- generation progress

### F4 — Business intelligence

- Analytics
- Revenue
- AI CFO
- channel portfolio

### F5 — Agent control plane

- Agent Control Center
- traces
- approvals
- budgets
- permissions
- workflow graphs

## Frontend non-goals

Do not introduce:

- Next.js only because it is popular
- SSR infrastructure without a measured requirement
- duplicated state stores
- provider-specific components spread across the UI
- direct AI API calls from browser code
- secret provider credentials in frontend code

All AI and YouTube credentials remain server-side.
