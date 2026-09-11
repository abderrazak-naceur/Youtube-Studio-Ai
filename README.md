# YouTube Studio AI

> **AI Media Company OS** — from an idea to a professional video, then from performance data to the next better idea.

YouTube Studio AI is being built as an AI-native operating system for creating, producing, publishing and improving original YouTube content at scale.

The core experience is deliberately simple:

**Idea / Prompt → Research → Script → Scene Plan → Visuals → Voice → Music/SFX → Captions → Edit/Render → QA → Professional MP4**

The long-term product goes beyond video generation. It connects content creation with opportunity discovery, channel intelligence, analytics and monetization so every published video can improve the next decision.

---

## Product vision

The project is designed to become a **company platform**, not just a video generator.

### Business loop

```text
FIND → RESEARCH → CREATE → PRODUCE → PUBLISH → MEASURE → LEARN → MONETIZE → SCALE
```

The primary optimization target is **net business value per published video**, not simply the number of videos or views.

### Core product areas

- **Market Intelligence** — trends, demand, competition, search intent and monetization signals.
- **Opportunity Engine** — identify and score promising content opportunities.
- **AI Research** — collect sources, evidence and claims with provenance.
- **Content Engine** — turn validated opportunities into briefs, scripts and scene plans.
- **Video Production** — generate and assemble visuals, voice, music/SFX, captions and final renders.
- **YouTube OS** — publishing, scheduling, channel management and platform analytics.
- **Community AI** — authorized comment intelligence and audience feedback loops.
- **Analytics Intelligence** — understand what changed, why it changed and what to do next.
- **Revenue Engine** — connect content performance to ads, affiliate, sponsorships, products and other revenue streams.
- **AI CEO / AI CFO** — strategic and financial decision support.
- **Learning Loop** — Content Genome + Opportunity Graph + historical performance data.

---

## Current status

The repository is currently in the **v0.1 MVP foundation / vertical-slice phase**.

The first objective is to prove a reliable production loop before adding large-scale automation or monetization features.

### Implemented foundation

- ASP.NET Core / .NET 10 backend
- C# domain model
- PostgreSQL + Entity Framework Core
- Workspace and channel foundations
- Opportunity model and API
- VideoProject model and API
- Persistent production jobs
- Video production job enqueue service
- Provider abstraction layer for research, script, scene planning, voice, visuals, music/SFX, captions, rendering and QA
- React + Vite + TypeScript frontend foundation
- Tailwind CSS
- Backend unit tests for the first video-project slice

### Current video pipeline

```text
Draft
  ↓
Researching
  ↓
Scripted
  ↓
Planned
  ↓
Producing
  ↓
  Voice + Visuals + Music/SFX
  ↓
Rendering
  ↓
QA
  ↓
Completed
```

The current implementation uses placeholders and provider interfaces where real AI credentials are not yet required. This keeps the architecture replaceable and testable while the core workflow is built. Music/SFX is represented as its own provider boundary and persisted production artifact before rendering.

---

## Technology stack

### Frontend

- Vite
- React
- TypeScript
- Tailwind CSS
- React Router
- TanStack Query
- Zustand
- Axios

### Backend

- ASP.NET Core
- .NET 10 LTS
- C#
- Entity Framework Core
- PostgreSQL / Npgsql
- Redis/Valkey for asynchronous coordination as the production architecture evolves
- REST API under `/api/v1`
- Background workers
- Provider adapters
- OpenTelemetry

### Infrastructure direction

The application is being designed to remain cloud-agnostic at the code level.

The current infrastructure strategy is:

- **2026:** DigitalOcean-first for efficient MVP development, with external AI providers.
- **2027:** hybrid architecture as usage and operational requirements grow.
- **2028+:** AWS-primary when scale, security, enterprise requirements and economics justify the move.

A direct AWS deployment can use RDS PostgreSQL, S3, SQS, ElastiCache/Redis, ECS/Fargate, CloudFront, ALB, CloudWatch, OpenTelemetry, Secrets Manager and IAM.

Kubernetes/EKS is intentionally not required for the MVP.

---

## Architecture principles

### 1. Modular monolith first

The MVP starts as a modular monolith plus workers. Services should only be separated when scale or ownership boundaries justify the operational cost.

### 2. Provider independence

AI and media providers are accessed through application-level interfaces. The core product must not depend on one vendor.

Planned provider categories include:

- LLM / reasoning
- research/search
- embeddings
- text-to-speech
- image generation
- video generation
- music/SFX
- transcription/captions
- rendering
- QA/moderation

### 3. No secrets in the browser

Provider credentials remain server-side. The frontend communicates with the application API and never receives provider API keys.

### 4. Cost is a first-class product metric

Every expensive AI/media operation should be attributable to the workspace, project and video, including provider, model, usage, unit price and timestamp.

### 5. Human control before publication

The system is designed with explicit QA and publication gates. Autonomous actions should be permissioned, auditable and reversible.

### 6. Originality and quality over volume

The goal is not mass-producing low-value videos. The system should help create original, useful and commercially sustainable content.

---

## Repository structure

```text
.
├── backend/
│   ├── src/
│   │   └── YoutubeStudio.Api/
│   └── tests/
│       └── YoutubeStudio.Api.Tests/
├── frontend/
├── docs/
│   ├── architecture/
│   ├── agents/
│   ├── ai/
│   ├── api/
│   ├── analytics/
│   ├── business-plan/
│   ├── cloud/
│   ├── company/
│   ├── content/
│   ├── database/
│   ├── finance/
│   ├── production/
│   ├── providers/
│   ├── revenue/
│   ├── roadmap/
│   ├── testing/
│   ├── ux-ui/
│   ├── workflows/
│   └── youtube/
└── README.md
```

The `docs/` directory contains the product, architecture, business, finance, cloud, AI, UX/UI, testing and implementation plans.

---

## Development workflow

Development follows an **Agile, dependency-driven release sequence**.

Each increment follows:

```text
Objective
  ↓
User Stories
  ↓
Tasks
  ↓
Implementation
  ↓
Tests
  ↓
Definition of Done
  ↓
Release
  ↓
Next increment
```

The current priority is to complete the MVP production vertical slice before moving into the broader YouTube OS and revenue features.

### Planned evolution

| Release | Focus |
|---|---|
| v0.1 | Foundation + video creation vertical slice |
| v0.2 | Opportunity Engine |
| v0.3 | Research Engine |
| v0.4 | Fact Check |
| v0.5 | Content Engine |
| v0.6 | AI Provider Layer |
| v0.7 | Production Engine |
| v0.8 | Complete video pipeline |
| v1.0 | MVP production loop |
| v1.x | YouTube publishing, analytics and learning loop |
| v2.0 | YouTube OS |
| v3.0 | Autonomous Creator / Creator Business OS |

The exact release boundaries may evolve as implementation evidence is collected, but dependencies and the production-first strategy remain the priority.

---

## Getting started

### Prerequisites

- .NET 10 SDK
- Node.js 20+ recommended
- npm
- PostgreSQL 16+ with the `vector` extension available for the planned AI/vector features

### Backend

```bash
cd backend/src/YoutubeStudio.Api
dotnet restore
dotnet run
```

The API uses the `/api/v1` route prefix.

Health check:

```text
GET /health
```

Swagger is available in the development environment.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The Vite development server runs on the standard local Vite port unless configured otherwise.

### Tests

From the backend test project:

```bash
cd backend/tests/YoutubeStudio.Api.Tests
dotnet test
```

> If PostgreSQL is not available locally, database-dependent integration tests should be run through the project's container/CI environment. The current unit tests use an isolated in-memory database where appropriate.

---

## Configuration and security

Local development configuration may use environment variables or .NET user secrets.

Do **not** commit:

- AI provider API keys
- YouTube OAuth client secrets
- YouTube refresh tokens
- database production passwords
- cloud credentials
- signing keys
- private user data

Production secrets should be stored in a dedicated secret manager such as AWS Secrets Manager or the equivalent service in the selected cloud environment.

---

## Quality gates

A production-ready increment should satisfy the project's Definition of Done, including as applicable:

- acceptance criteria implemented
- unit/integration tests added
- API contracts stable and versioned
- persistence verified
- failure paths handled
- provider boundaries preserved
- no secrets exposed
- cost attribution preserved
- observability added for background jobs
- documentation updated
- CI/build checks passing

For the video pipeline, QA will ultimately cover technical validity, editorial quality, factual confidence, source provenance, originality/reuse risk, rights/licensing, safety and publication readiness.

---

## Long-term roadmap

### 2026 — MVP Production Engine

Prove:

**idea → research → script → scene plan → voice → visuals → music/SFX → edit → captions → thumbnail → metadata → MP4**

### 2027 — YouTube OS

Add OAuth, publishing, scheduling, analytics, comments, Shorts, SEO, experiments and channel intelligence.

### 2028 — Revenue Intelligence

Connect content to profitability, affiliate revenue, sponsorships, products, forecasts and portfolio optimization.

### 2029 — Autonomous Creator

Introduce permissioned agents that can discover opportunities, prepare content, request approval, publish approved work, inspect results and recommend the next action.

### 2030 — Creator Business / AI Media Company OS

Expand from YouTube into owned media, websites, newsletters, products, memberships, commerce and multiple distribution channels.

---

## Product philosophy

YouTube Studio AI should feel like a **professional creative operating system**, not a collection of AI chat boxes.

The product should continuously answer five questions:

1. **What should we create?**
2. **Why is this opportunity worth pursuing?**
3. **How do we produce it professionally?**
4. **Did it create business value?**
5. **What should we do next?**

The long-term moat is expected to come from the combination of:

**Channel DNA + Content Genome + Opportunity Graph + performance history + cost model + revenue model + workflow intelligence.**

---

## License

License and contribution rules will be finalized as the project approaches public distribution.
