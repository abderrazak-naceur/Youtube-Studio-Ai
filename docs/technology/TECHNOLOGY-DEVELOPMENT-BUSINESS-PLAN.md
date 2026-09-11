# Technology Development Business Plan — 2026–2030

## 1. Purpose

This plan translates the company strategy into an executable technology investment plan for frontend, backend, AI, media, infrastructure and engineering operations.

The objective is not to maximize technology complexity. The objective is to build the smallest technology system capable of producing measurable business value and then compound the resulting data into a moat.

## 2. Technology company thesis

The product owns:

- workflow orchestration
- domain model
- Channel DNA
- Content Genome
- Opportunity Graph
- Model Router
- cost model
- revenue model
- agent permissions
- historical performance data

Third-party providers remain replaceable.

## 3. Technology stack

### Frontend

**Vite + React + TypeScript + Tailwind CSS**

Core libraries:

- React Router
- TanStack Query
- Zustand
- Axios
- Zod
- React Hook Form
- SignalR client
- Radix UI
- Lucide
- Motion
- React Markdown
- React Flow
- Monaco Editor

### Backend

**ASP.NET Core + .NET 10 LTS + C#**

Core:

- EF Core
- PostgreSQL/Npgsql
- Redis/Valkey
- REST API
- SignalR
- background workers
- provider adapters
- OpenTelemetry

### AI

Internal Model Router with adapters for:

- OpenAI
- Hugging Face
- Higgsfield
- AWS Bedrock
- TTS/image/video providers

### Media

- FFmpeg
- containerized render workers
- object storage
- thumbnails
- captions

### Infrastructure

- Docker
- DigitalOcean in 2026
- AWS progressively from 2027
- Terraform/OpenTofu
- GitHub Actions
- OpenTelemetry

## 4. Why Vite for this company

Vite is the right frontend choice for the current architecture because the application is an authenticated SaaS control plane rather than an SEO-first public website.

The product needs:

- rich client-side interactions
- dashboards
- editors
- timelines
- real-time progress
- agent graphs
- analytics
- media preview
- keyboard-heavy workflows

Vite keeps the frontend simple and lets ASP.NET Core remain the authoritative backend.

Vite provides fast HMR and optimized production builds, and React's official documentation includes Vite as a recommended route for building React applications from scratch. citeturn0search2turn0search10

## 5. Product architecture

```text
                     Browser
                        │
            Vite + React + TypeScript
                        │
             REST + SignalR + uploads
                        │
                        ▼
                 ASP.NET Core API
                        │
        ┌───────────────┼────────────────┐
        ▼               ▼                ▼
   PostgreSQL       Redis/Valkey     Object Storage
        │               │                │
        └───────────────┼────────────────┘
                        ▼
                      Queue
                        │
             ┌──────────┼──────────┐
             ▼          ▼          ▼
          AI Jobs    Media Jobs  Analytics
             │          │          │
             ▼          ▼          ▼
        AI Providers   FFmpeg   YouTube APIs
```

## 6. 2026 — Build the engine

### Goal

Prove that one person/small team can repeatedly produce high-quality original videos at predictable cost.

### Frontend

Build:

1. AppShell
2. Dashboard
3. Opportunities
4. Research
5. Content project
6. Script editor
7. Scene planner
8. Asset library
9. Production progress
10. QA/review

### Backend

Build:

1. Identity/workspaces
2. Channel DNA
3. Opportunity engine
4. Research engine
5. Content engine
6. Job system
7. Asset system
8. Provider adapters
9. Render pipeline
10. Cost events
11. Audit events

### Exit criteria

- complete video can move through the pipeline
- every job is observable
- every AI action has a cost
- failed jobs can retry
- final MP4 is produced
- human QA is enforced

## 7. 2027 — Build the YouTube OS

### Frontend

- channel management
- content calendar
- publishing center
- analytics dashboards
- comment intelligence
- experiments
- mobile approval views

### Backend

- YouTube OAuth
- upload/scheduling
- metadata management
- analytics ingestion
- comments
- experiments
- notifications
- stronger workflow durability

### Business objective

Turn the production engine into a usable operating system for multiple channels.

## 8. 2028 — Monetize the intelligence

### Frontend

- Revenue dashboard
- AI CFO
- profitability by video
- monetization recommendations
- sponsorship pipeline
- affiliate intelligence
- portfolio allocation

### Backend

- revenue events
- cost attribution
- conversion attribution
- forecasting
- customer billing/usage
- revenue experiments

### Business objective

Move from "we can create videos" to "we know which videos and channels create the most business value."

## 9. 2029 — Autonomous Creator

### Frontend

- Agent Control Center
- approval inbox
- agent budgets
- execution traces
- policies
- rollback controls

### Backend

- durable agent workflows
- tool permissions
- bounded autonomy
- policy engine
- human approvals
- execution audit
- rollback/recovery

### Business objective

Allow the platform to operate repetitive parts of the media business while humans retain control over strategic and sensitive decisions.

## 10. 2030 — AI Media Company OS

### Frontend

- multi-channel portfolio cockpit
- business planning
- international markets
- products
- newsletters
- memberships
- commerce
- enterprise administration

### Backend

- portfolio optimization
- multi-channel intelligence
- advanced analytics platform
- enterprise tenancy
- marketplace APIs
- intelligence APIs

### Business objective

The platform becomes the operating system of a media company, not merely a video generator.

## 11. Investment priorities

### Highest priority

1. Product workflow
2. Data model
3. Job orchestration
4. AI provider abstraction
5. Media pipeline
6. QA and compliance
7. Cost tracking
8. Analytics learning loop

### Medium priority

- advanced visualization
- dedicated search infrastructure
- sophisticated workflow engine
- enterprise features
- mobile application

### Defer until justified

- GPU fleet
- large microservice estate
- service mesh
- custom LLM training
- dedicated vector database
- complex data warehouse
- multi-cloud active-active architecture

## 12. Team plan

### 2026

Founder/technical product owner + 1–3 engineers/contractors as needed.

Suggested split:

- founder: product, architecture, business and critical engineering
- frontend: Vite/React/Tailwind
- backend: .NET/PostgreSQL/workers
- AI/media contractor: provider and rendering integrations

### 2027

Add:

- backend/platform engineer
- frontend/product engineer
- AI/data engineer
- product designer

### 2028

Add:

- data/analytics engineer
- DevOps/cloud engineer
- QA automation
- growth/product operations

### 2029–2030

Create separate engineering ownership for:

- SaaS platform
- AI platform
- media production
- data intelligence
- infrastructure/security

## 13. Engineering economics

The company should measure:

`Engineering Cost / Net Business Value Created`

and not simply:

`Lines of Code / Features Delivered`.

Every major feature must answer:

- what business problem does it solve?
- what revenue or cost lever does it affect?
- what infrastructure cost does it introduce?
- what data does it create for the learning loop?
- can it be removed or replaced later?

## 14. Technology budget planning

Illustrative internal planning ranges:

| Phase | Software/cloud engineering envelope | Main driver |
|---|---:|---|
| 2026 MVP | €2k–€15k/month | people + cloud + AI/media usage |
| 2027 YouTube OS | €15k–€50k/month | team + integrations + AI/media scale |
| 2028 Revenue Intelligence | €50k–€150k/month | team + data + infrastructure + AI |
| 2029 Autonomous Creator | €100k–€300k+/month | engineering + agents + reliability |
| 2030 Media Company OS | €200k–€600k+/month | platform + enterprise + media scale |

These are planning envelopes, not financial forecasts. AI generation and media usage should remain separately metered because usage can grow much faster than fixed infrastructure.

## 15. Technology KPIs

### Delivery

- lead time for changes
- deployment frequency
- change failure rate
- mean time to recovery

### Product

- opportunity-to-video cycle time
- video approval cycle time
- successful render rate
- publish success rate

### AI

- AI cost/video
- provider success rate
- model quality score
- retry rate
- latency

### Business

- contribution margin/video
- revenue/video
- net business value/video
- engineering cost/revenue
- infrastructure cost/revenue

## 16. Final strategic decision

**Frontend:** Vite + React + TypeScript + Tailwind CSS.

**Backend:** ASP.NET Core + .NET 10 LTS + C#.

**Database:** PostgreSQL + pgvector.

**Real-time:** SignalR.

**Async:** queue abstraction + workers; SQS later where justified.

**Media:** FFmpeg + dedicated workers.

**AI:** internal Model Router + replaceable providers.

**Cloud:** DigitalOcean-first in 2026, hybrid from 2027, AWS-primary only when justified.

This stack gives the company a fast, cost-conscious path to the 2030 AI Media Company OS without locking the business into a cloud vendor or AI provider.
