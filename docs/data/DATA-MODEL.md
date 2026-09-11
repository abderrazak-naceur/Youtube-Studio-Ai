# Data Model

## 1. Core entities

### Workspace
Tenant boundary containing users, channels, settings and billing data.

### User
Authenticated person with roles and permissions.

### Channel
YouTube or planned channel with identity, niche, language and operational configuration.

### Channel DNA
Structured representation of audience, positioning, tone, visual language, recurring formats, forbidden patterns and strategic goals.

### Opportunity
Potential content topic with evidence, score, estimated effort and monetization potential.

### Research Project
Sources, notes, claims, transcripts and fact-check results for an opportunity.

### Script
Versioned narrative generated from approved research.

### Scene
Timed production unit connected to script segments and assets.

### Asset
Image, video, audio, music, SFX, caption, thumbnail or other media artifact.

### Video Project
Complete production state from idea to final render.

### Render
A concrete generated video output with settings, source assets and cost.

### Publication
YouTube upload/schedule state, metadata and publication result.

### Analytics Snapshot
Time-series metrics associated with a video/channel.

### Experiment
A controlled change to title, thumbnail, format, hook or other variable.

### Revenue Event
Revenue or conversion attributed to a content asset or channel.

### Cost Event
Provider, compute, storage or operational cost attributable to a workflow.

## 2. Relationships

```text
Workspace
 ├── Users
 ├── Channels
 │    └── Channel DNA
 └── Projects
      └── Opportunity
           └── Research
                └── Script
                     └── Scenes
                          └── Assets
                               └── Render
                                    └── Publication
                                         └── Analytics
                                              └── Experiments
                                                   └── Revenue / Cost
```

## 3. Versioning

The following must be versioned:
- Channel DNA
- prompts
- scripts
- scene plans
- asset manifests
- metadata
- renders
- publication configuration
- scoring models

Never silently overwrite a meaningful AI-generated artifact.

## 4. Events

Recommended domain events:
- `opportunity.created`
- `opportunity.scored`
- `research.completed`
- `factcheck.completed`
- `script.approved`
- `render.completed`
- `quality_gate.failed`
- `video.approved`
- `publication.completed`
- `analytics.updated`
- `experiment.completed`
- `revenue.recorded`
- `cost.recorded`

## 5. Data quality

Every external fact should retain source provenance. Every generated media artifact should retain provider, model/tool, timestamp and licensing/provenance metadata where available.

## 6. Multi-tenant isolation

All tenant-owned records must carry an unambiguous workspace ownership boundary. Authorization must be enforced at the application and data-access layers.
