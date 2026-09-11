# System Architecture

## 1. Architectural goal

Build a modular platform where AI orchestration, media processing, YouTube operations, analytics and monetization can evolve independently.

## 2. Logical architecture

```text
Web UI
  ↓
API / Application Layer
  ↓
Workflow Orchestrator
  ├── Opportunity Engine
  ├── Research + Fact Check
  ├── Content Engine
  ├── Production Engine
  ├── Publishing Engine
  ├── Analytics Engine
  ├── Revenue Engine
  └── Agent / Strategy Layer
  ↓
Domain Services + Job Workers
  ↓
PostgreSQL | Object Storage | Queue | Cache | Search
  ↓
External Providers
  ├── LLMs
  ├── TTS
  ├── Image / Video generation
  ├── Music / SFX
  ├── Rendering
  └── YouTube APIs
```

## 3. Recommended boundaries

### Application layer
Authentication, API endpoints, commands, validation and orchestration requests.

### Domain layer
Channel, idea, research, script, video, asset, publication, analytics and revenue entities.

### AI layer
Provider adapters, model router, prompts, structured outputs, safety checks and AI observability.

### Media layer
Asset acquisition, normalization, timeline assembly, rendering, subtitles and thumbnails.

### Integration layer
YouTube, provider APIs, storage, search and external services.

## 4. Asynchronous architecture

Long-running operations must run as jobs rather than blocking HTTP requests.

Example:

`CreateVideoJob → ResearchJob → ScriptJob → SceneJobs → AssetJobs → VoiceJob → RenderJob → QualityGateJob`

Each job must have:
- job ID
- entity ID
- status
- attempt count
- provider/model
- started/finished timestamps
- cost where applicable
- error information
- idempotency key

## 5. Storage

### PostgreSQL
System of record for structured application state.

### Object storage
Raw and generated media: audio, video, images, thumbnails, subtitles and intermediate files.

### Queue
Reliable execution of long-running work.

### Cache
Short-lived performance data and provider responses where legally and technically appropriate.

### Search/index
Research sources, transcripts, comments and semantic content retrieval.

## 6. Observability

Every workflow should expose:
- execution status
- latency
- provider/model
- token or generation usage
- estimated cost
- retries
- final artifact
- validation outcome

## 7. Scalability strategy

Start as a modular monolith with worker processes. Do not introduce microservices prematurely.

Split services only when there is a demonstrated need for independent scaling, deployment or ownership.

## 8. Reliability

- Retry transient provider failures.
- Never retry non-idempotent publishing blindly.
- Persist workflow state after every meaningful stage.
- Use timeouts and circuit breakers for external providers.
- Preserve failed artifacts for debugging when safe.
- Support resumable production.
