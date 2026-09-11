# Backend Technology Business Plan

## 1. Business objective

The backend is the economic and operational core of YouTube Studio AI. It must support the SaaS business, owned media business, AI production engine and future autonomous creator.

The backend must optimize for:

- correctness
- security
- predictable operating cost
- asynchronous AI/media workloads
- provider portability
- multi-tenant isolation
- auditability
- ability to scale without an early microservice tax

## 2. Backend decision

**ASP.NET Core on .NET 10 LTS + C#**.

.NET 10 is currently an active LTS release with support through November 14, 2028. citeturn0search11turn0search13

## 3. Architecture strategy

Start with a **modular monolith + workers**.

```text
                    API
                     │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
   Application     Domain     Integrations
       │             │             │
       └─────────────┼─────────────┘
                     ▼
              Infrastructure
                     │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
 PostgreSQL       Cache          Object Storage
                     │
                     ▼
                  Queue
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
   AI Workers   Media Workers  Analytics Workers
```

Do not split the system into microservices in the MVP. The first split should happen only when a workload has independent scaling, reliability, deployment or ownership requirements.

## 4. Backend domains

### Identity

- users
- workspaces
- memberships
- roles
- permissions
- sessions
- connected accounts

### Content business

- channels
- channel DNA
- opportunities
- research
- sources
- claims
- scripts
- scenes
- content projects

### Production

- assets
- providers
- jobs
- renders
- captions
- voice
- media timelines

### YouTube

- OAuth connections
- publications
- schedules
- video metadata
- channel analytics
- comments

### Intelligence

- Content Genome
- Opportunity Graph
- analytics snapshots
- experiments
- recommendations

### Finance

- cost events
- revenue events
- attribution
- profitability
- budgets
- forecasts

### Governance

- audit events
- approvals
- rights/provenance
- safety gates
- policy decisions

## 5. API strategy

Base path:

`/api/v1`

Use:

- REST for business resources
- SignalR for real-time progress/events
- async jobs for long-running operations
- idempotency keys for generation/publishing
- cursor pagination
- request IDs
- structured error codes
- optimistic concurrency where required

The backend must never expose AI provider credentials to the browser.

## 6. Project structure

```text
src/
├── YoutubeStudio.Api/
├── YoutubeStudio.Application/
├── YoutubeStudio.Domain/
├── YoutubeStudio.Infrastructure/
├── YoutubeStudio.Integrations/
├── YoutubeStudio.Workers/
└── YoutubeStudio.Shared/
```

### Domain

Pure business rules and entities. No HTTP, database or provider SDK dependencies.

### Application

Use cases, commands, queries, validation and orchestration.

### Infrastructure

EF Core, PostgreSQL, cache, object storage, queues and telemetry.

### Integrations

Provider adapters:

- YouTube
- OpenAI
- Hugging Face
- Higgsfield
- AWS
- TTS/image/video providers

### Workers

Separate executable processes for expensive asynchronous work.

## 7. Database strategy

**PostgreSQL** is the system of record.

Use EF Core + Npgsql.

Tenant-owned records must contain or strongly inherit `workspace_id`.

Core entities:

```text
User
Workspace
Membership
Channel
ChannelDna
Opportunity
ResearchProject
Source
Claim
Script
Scene
Asset
VideoProject
Job
Render
Publication
AnalyticsSnapshot
Experiment
RevenueEvent
CostEvent
AuditEvent
```

Use migrations in source control and require migration/rollback validation in CI.

## 8. Job architecture

Long-running operations are jobs, not HTTP requests.

Example states:

`Pending → Running → Succeeded`

or

`Pending → Running → Failed → Retry → Running`

Every job needs:

- idempotency key
- retry policy
- timeout
- budget
- provider/model
- progress
- error classification
- output references
- actual cost
- audit trace

## 9. Queue evolution

### MVP

Use PostgreSQL outbox + Redis/Valkey-backed queue/worker coordination where practical.

### Growth

Introduce a durable managed queue such as Amazon SQS when AWS becomes primary.

### Long-running autonomous workflows

Evaluate Temporal when workflows require durable timers, retries, compensation, human approvals and multi-day execution.

The application depends on an internal queue/workflow interface, not directly on a cloud vendor.

## 10. AI gateway

All model calls pass through an internal AI Gateway and Model Router.

```text
Use Case
  ↓
AI Gateway
  ↓
Policy / Budget / Rights checks
  ↓
Model Router
  ↓
Provider Adapter
  ↓
Provider API
```

Store:

- provider
- model
- input/output usage
- latency
- estimated cost
- actual cost
- success/failure
- quality evaluation

This data feeds the Model Router and AI CFO.

## 11. Real-time architecture

Use ASP.NET Core SignalR for:

- video render progress
- AI generation progress
- agent state changes
- approval requests
- publishing state
- system notifications

SignalR has an official JavaScript client and is designed for real-time communication in ASP.NET Core. citeturn1search1turn1search5

## 12. Security

Required controls:

- OIDC/OAuth
- short-lived access tokens
- secure refresh token rotation
- encrypted YouTube refresh tokens
- role/workspace authorization
- server-side secret storage
- audit logs
- rate limiting
- request validation
- SSRF protection for external URL ingestion
- upload validation
- malware scanning where required
- least-privilege provider credentials

## 13. Observability

Use OpenTelemetry for traces, metrics and logs. The .NET implementation supports stable traces, metrics and logs. citeturn1search0turn1search8

Every request should be traceable across:

`HTTP → application use case → job → provider → asset → render → QA → publish`.

## 14. Testing strategy

### Unit

Business rules, state transitions, scoring and financial calculations.

### Integration

PostgreSQL, cache, queue, object storage and provider adapters.

### Contract

Verify each AI/provider adapter against expected request/response contracts.

### Workflow

Test complete video lifecycle and recovery from failures.

### Security

Authentication, authorization, tenant isolation, upload and webhook tests.

### E2E

Critical business journeys with Playwright or equivalent. Playwright supports .NET and xUnit-based E2E testing. citeturn1search2turn1search4

## 15. Backend delivery phases

### B1 — Foundation

- solution structure
- API versioning
- PostgreSQL
- EF Core migrations
- authentication
- workspace/tenant model
- structured errors
- logging/telemetry
- CI

### B2 — Content engine

- opportunities
- research
- claims
- scripts
- scenes
- Content Genome

### B3 — Production engine

- assets
- provider adapters
- jobs
- workers
- FFmpeg
- render lifecycle
- QA

### B4 — YouTube OS

- OAuth
- publishing
- scheduling
- analytics
- comments
- experiments

### B5 — Revenue Intelligence

- cost attribution
- revenue ingestion
- profitability
- forecasts
- AI CFO

### B6 — Autonomous Creator

- durable workflows
- permissions
- agent budgets
- approvals
- execution traces
- rollback/recovery

## 16. Backend cost strategy

Keep fixed backend infrastructure low in 2026.

Target architecture:

- one API deployment
- one worker deployment
- one PostgreSQL instance
- one cache
- object storage
- external AI providers

Scale expensive media/AI workers independently rather than scaling the API blindly.

## 17. Backend KPIs

- API p95 latency
- job queue latency
- job success rate
- provider failure rate
- retry rate
- render success rate
- cost per video
- cost per AI action
- publication success rate
- incident frequency
- database query latency

## 18. Business principle

The backend is not just an API. It is the **operating system for the company's content business**.

It must own the data, workflows, economics, permissions and learning loop while keeping cloud and AI providers replaceable.
