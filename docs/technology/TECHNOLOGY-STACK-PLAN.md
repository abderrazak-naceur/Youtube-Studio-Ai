# YouTube Studio AI — Technology Stack Plan

## 1. Objective

Define the technology stack for the company platform from MVP to a multi-channel AI Media Company OS.

The stack must optimize for:

- fast product delivery in 2026
- low fixed infrastructure cost before product-market fit
- strong support for AI and media workloads
- provider portability
- tenant isolation
- observable asynchronous workflows
- secure YouTube integrations
- a migration path from DigitalOcean to AWS without rewriting the domain layer

## 2. Recommended stack at a glance

| Layer | Recommended technology | Role |
|---|---|---|
| Web app | Next.js + React + TypeScript | SaaS UI, dashboards, creator workflows |
| UI | Tailwind CSS + shadcn/ui + Radix primitives | Design system and accessible components |
| Client state | TanStack Query + URL state | Server state, caching and navigation state |
| Validation | Zod | Frontend/API contract validation |
| Backend API | ASP.NET Core on .NET 10 LTS + C# | Core business API and integrations |
| ORM | EF Core | PostgreSQL access and migrations |
| Database | PostgreSQL | System of record |
| Cache | Redis-compatible cache / Valkey | Cache, locks, rate limiting, transient state |
| Jobs | Queue abstraction; SQS on AWS, managed queue equivalent on DigitalOcean | Async AI/media jobs |
| Workflow | Application workflow engine initially; Temporal when durable long-running workflows justify it | Video and agent orchestration |
| Object storage | S3-compatible abstraction; DigitalOcean Spaces first, AWS S3 later/when justified | Video, audio, images, thumbnails, subtitles |
| Search | PostgreSQL full-text + pgvector initially; dedicated search later if required | Research, semantic retrieval, content intelligence |
| AI router | Internal Model Router | Provider selection, cost, quality, latency and safety |
| LLM providers | OpenAI / AWS Bedrock / Hugging Face and other adapters | Research, scripts, agents, classification |
| Video generation | Higgsfield + provider adapters | Premium video/visual generation |
| Open models | Hugging Face Inference Providers / dedicated endpoints where justified | Experimentation and alternative models |
| Media processing | FFmpeg in dedicated worker containers | Assembly, transcoding, audio/video normalization, captions |
| Rendering | Containerized render workers | Final MP4 generation |
| Authentication | OIDC/OAuth + secure application sessions | Users, workspaces and integrations |
| YouTube | YouTube Data API + YouTube Analytics API | Publishing, channel data and analytics |
| Observability | OpenTelemetry + Grafana-compatible metrics/logs/traces | Platform and AI observability |
| Error tracking | Sentry-compatible integration | Application and worker failures |
| CI/CD | GitHub Actions | Build, test, security checks and deployment |
| Containers | Docker | Portable deployment unit |
| Infrastructure | Terraform/OpenTofu | Reproducible infrastructure |
| Secrets | Cloud secret manager / encrypted secret store | API keys and credentials |
| Testing | xUnit + integration tests + Playwright + AI eval fixtures | Backend, workflow and UI quality |

## 3. Frontend

### Primary choice

**Next.js + React + TypeScript**.

The application should use the App Router and a component-driven architecture. Next.js is the web application framework; business logic remains in the backend rather than becoming scattered across UI components.

### UI stack

- Tailwind CSS
- shadcn/ui
- Radix primitives where accessibility behavior is needed
- Lucide icons
- TanStack Query
- React Hook Form
- Zod
- Playwright for browser E2E

### Frontend structure

```text
apps/web
├── app/
├── components/
│   ├── layout/
│   ├── dashboard/
│   ├── opportunities/
│   ├── research/
│   ├── creator/
│   ├── production/
│   ├── analytics/
│   ├── revenue/
│   └── agents/
├── features/
├── lib/
├── hooks/
└── styles/
```

The UI should consume versioned API contracts and never contain provider-specific AI logic.

## 4. Backend

### Primary choice

**ASP.NET Core on .NET 10 LTS + C#**.

The backend is the business system of record and should own:

- authentication and authorization
- workspaces and tenancy
- channels
- opportunities
- research projects
- scripts
- assets
- video projects
- jobs
- publishing
- analytics normalization
- revenue and cost accounting
- agent permissions
- audit logs

### Architecture

Use a modular monolith first:

```text
src/
├── Api/
├── Application/
├── Domain/
├── Infrastructure/
├── Integrations/
├── Workers/
└── Shared/
```

Split into independently deployed services only when scale, ownership or reliability requires it.

## 5. Database

### PostgreSQL

PostgreSQL is the primary system of record.

Main domains:

- users
- workspaces
- memberships
- channels
- channel_dna
- opportunities
- research_projects
- sources
- claims
- scripts
- scenes
- assets
- video_projects
- jobs
- renders
- publications
- analytics_snapshots
- experiments
- revenue_events
- cost_events
- audit_events

Use:

- UUID identifiers
- UTC timestamps
- foreign-key constraints
- explicit state transitions
- tenant ownership
- immutable audit events
- soft deletion where required

Large binaries never live in PostgreSQL.

## 6. Vector and semantic search

Start with **pgvector** inside PostgreSQL for:

- research source embeddings
- transcript embeddings
- comment clustering
- content similarity
- Content Genome retrieval
- opportunity similarity

Introduce a dedicated vector/search system only when measured scale or query latency requires it.

## 7. Async jobs and workflows

AI and media operations must never depend on a long-running HTTP request.

Example:

```text
Create Video
    ↓
Research Job
    ↓
Fact Check Job
    ↓
Script Job
    ↓
Scene Jobs
    ↓
Visual / Image / Video Jobs
    ↓
Voice Job
    ↓
Render Job
    ↓
Technical QA
    ↓
Editorial QA
    ↓
Publication Gate
    ↓
YouTube Publish
    ↓
Analytics Ingestion
```

Every job stores:

- job_id
- workspace_id
- project_id
- video_id
- status
- attempt_count
- idempotency_key
- provider
- model
- usage
- estimated_cost
- actual_cost
- timestamps
- error
- output references

### Queue strategy

Use a queue abstraction in the application.

- DigitalOcean phase: managed queue or Redis/Valkey-backed worker queue depending on workload.
- AWS phase: Amazon SQS for durable decoupling and scaling.

Do not make business logic depend directly on SQS APIs.

## 8. AI architecture

All external AI calls go through the internal **Model Router**.

```text
Application
    ↓
AI Gateway
    ↓
Model Router
    ├── OpenAI adapter
    ├── AWS Bedrock adapter
    ├── Hugging Face adapter
    ├── Higgsfield adapter
    ├── TTS adapters
    ├── Image adapters
    └── Video adapters
```

The router selects providers based on:

1. hard safety/rights constraints
2. output requirements
3. quality target
4. customer plan
5. budget
6. latency
7. reliability
8. historical performance
9. provider availability

Every request records actual usage and cost.

## 9. Media pipeline

Use dedicated worker containers for media processing.

### FFmpeg responsibilities

- video assembly
- audio mixing
- codec conversion
- normalization
- subtitles/captions
- thumbnails and frame extraction
- quality validation
- final MP4 packaging

Media workers should be isolated from the API process because CPU, memory and disk usage can be unpredictable.

## 10. Cloud deployment plan

### 2026 — DigitalOcean first

```text
Internet
   ↓
DigitalOcean Load Balancer
   ↓
Docker / DOKS
   ├── Web
   ├── API
   └── Workers
        ↓
PostgreSQL
Redis/Valkey
Spaces
External AI Providers
```

Keep AI generation external initially rather than buying and operating GPU infrastructure too early.

### 2027 — Hybrid

```text
DigitalOcean
├── Product workloads
└── Cost-sensitive workers

AWS
├── Selected managed services
├── Enterprise workloads
└── Security/compliance-sensitive workloads
```

### 2028+ — AWS-primary where justified

```text
CloudFront
   ↓
Load Balancer
   ↓
ECS/EKS
   ├── Web/API
   └── Workers
        ↓
RDS PostgreSQL
ElastiCache
S3
SQS/EventBridge
CloudWatch/OpenTelemetry
Bedrock + external providers
```

DigitalOcean remains useful for isolated workloads, development, cost-sensitive services or redundancy.

## 11. Infrastructure as Code

Use **Terraform or OpenTofu** from the beginning.

Infrastructure modules should cover:

- networking
- databases
- cache
- object storage
- Kubernetes/Docker workloads
- queues
- monitoring
- secrets
- DNS
- environments

No production resource should exist only because it was manually created in a web console.

## 12. Authentication and security

Use:

- OIDC/OAuth
- short-lived access tokens
- secure refresh-token rotation
- encrypted YouTube refresh tokens
- workspace-level authorization
- role-based permissions
- audit logging
- least-privilege provider credentials
- encrypted storage and transport
- secret rotation

Sensitive operations such as publishing require explicit permission and an audit trail.

## 13. Observability

OpenTelemetry should be the common telemetry layer.

Trace:

```text
HTTP request
 → workflow
 → job
 → AI provider call
 → asset
 → render
 → QA
 → publication
```

Record:

- latency
- failures
- retries
- tokens/usage
- provider/model
- estimated cost
- actual cost
- queue time
- render duration

The system should answer: **which provider, model and workflow step cost how much for this video?**

## 14. Testing

### Backend

- xUnit
- integration tests
- database tests
- provider contract tests
- workflow tests

### Frontend

- component tests
- Playwright E2E
- accessibility checks

### AI

Evaluate:

- factuality
- relevance
- originality
- narrative quality
- visual consistency
- instruction adherence
- safety
- cost efficiency
- latency

## 15. Repository structure target

```text
Youtube-Studio-Ai/
├── apps/
│   └── web/
├── src/
│   ├── Api/
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   ├── Integrations/
│   ├── Workers/
│   └── Shared/
├── tests/
│   ├── Unit/
│   ├── Integration/
│   ├── E2E/
│   ├── AI/
│   └── Contracts/
├── infra/
│   ├── terraform/
│   ├── docker/
│   └── environments/
├── docs/
└── scripts/
```

## 16. Development environments

### Local

Docker Compose should provide:

- PostgreSQL
- Redis/Valkey
- local object storage emulator where useful
- API
- worker
- web

### Development

Shared DigitalOcean environment with isolated database/schema and provider budgets.

### Staging

Production-like infrastructure with sandbox/test YouTube credentials and restricted AI budgets.

### Production

Separate cloud account/project, separate secrets, backups, monitoring and strict publication permissions.

## 17. Technology decisions by phase

### Phase 1 — MVP

Use:

- Next.js
- TypeScript
- Tailwind/shadcn
- ASP.NET Core .NET 10
- PostgreSQL
- Redis/Valkey
- Docker
- DigitalOcean
- FFmpeg workers
- Model Router
- OpenAI + Hugging Face + Higgsfield adapters
- GitHub Actions
- OpenTelemetry

Avoid:

- microservices
- self-hosted LLMs at scale
- complex Kubernetes service meshes
- custom GPU clusters
- dedicated vector databases
- event-platform complexity without measured need

### Phase 2 — YouTube OS

Add:

- OAuth/YouTube integration
- scheduled jobs
- analytics ingestion
- comment intelligence
- experiments
- stronger workflow durability
- AWS services where useful

### Phase 3 — Revenue Intelligence

Add:

- attribution pipeline
- profitability engine
- forecasting
- affiliate/sponsorship/product integrations
- warehouse/analytics infrastructure if PostgreSQL is no longer sufficient

### Phase 4 — Autonomous Creator

Add:

- durable agent workflows
- approval policies
- agent budgets
- tool permissions
- execution traces
- rollback/recovery
- bounded autonomy

### Phase 5 — AI Media Company OS

Add:

- multi-channel portfolio optimization
- internationalization
- enterprise tenancy
- advanced data platform
- dedicated AI inference where economics justify it
- marketplace and intelligence APIs

## 18. Final technology principle

The company should own the **orchestration, domain model, workflow intelligence, cost model, Content Genome, Opportunity Graph and business data**.

Cloud vendors and AI providers should remain replaceable.

The durable architecture is therefore:

**Next.js + TypeScript → ASP.NET Core/.NET 10 → PostgreSQL → async workers → Model Router → external AI providers → FFmpeg/media workers → YouTube → Analytics → Revenue → Learning Loop.**
