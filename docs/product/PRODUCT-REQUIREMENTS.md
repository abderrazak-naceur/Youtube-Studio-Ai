# Product Requirements — Youtube Studio AI

## 1. Product definition

Youtube Studio AI is an AI-native operating system for researching, producing, publishing and monetizing YouTube content.

It is not primarily a video generator. Video generation is one component inside a larger decision and learning system.

## 2. North Star

**Net business value per published video.**

Conceptually:

`Expected revenue + strategic value - production cost - distribution cost - risk cost`

The exact financial formula will evolve as real channel data becomes available.

## 3. Primary users

### Creator
Needs to find good topics, create better videos, save time and understand performance.

### Professional creator
Needs repeatable workflows, channel intelligence, experiments, monetization and operational control.

### Agency
Needs multiple clients, channels, permissions, approvals, reporting and cost allocation.

### Media company
Needs portfolio management, automation, forecasting and cross-channel optimization.

## 4. Core user journey

1. Create workspace.
2. Define channel identity and Channel DNA.
3. Select niche/audience/language.
4. Discover opportunities.
5. Score opportunities.
6. Research selected topic.
7. Verify facts and sources.
8. Generate script.
9. Generate scene plan.
10. Generate/acquire assets.
11. Generate voice and edit.
12. Generate captions, thumbnail and metadata.
13. Run publication safety gate.
14. Human approval.
15. Publish/export.
16. Collect performance data.
17. Analyze results.
18. Update Content Genome and Opportunity Graph.
19. Recommend next actions.

## 5. MVP requirements

### P0 — mandatory
- Project/workspace creation.
- Channel profile.
- Channel DNA.
- Idea intake.
- Research workflow.
- Source collection.
- Fact-check status.
- Script generation.
- Scene planning.
- Asset manifest.
- Voice generation interface.
- Video rendering pipeline.
- Captions.
- Thumbnail generation interface.
- Metadata generation.
- Quality gate.
- Final MP4 export.
- Per-video cost tracking.
- Audit trail.

### P1 — after first successful production loop
- Opportunity scoring.
- Model routing.
- Content Genome.
- Batch production.
- Templates.
- Revision workflow.
- Production queue.
- Analytics import.

### P2 — 2027+ platform layer
- YouTube OAuth.
- Upload and scheduling.
- Channel analytics.
- Comments intelligence.
- Shorts engine.
- Experiments.
- Monetization intelligence.

## 6. Non-functional requirements

- Idempotent jobs where possible.
- Retryable asynchronous workers.
- Structured logs.
- Traceable AI calls.
- Versioned prompts.
- Versioned generated artifacts.
- Secure secret handling.
- Tenant isolation.
- Configurable model providers.
- Reproducible rendering.
- Cost observability.

## 7. Product rules

- Never publish automatically in the early MVP.
- Never fabricate sources or citations.
- Never hide uncertainty from the user.
- Never use fake engagement.
- Never intentionally produce repetitive mass-produced low-value content.
- High-risk topics require stronger review.
- AI-generated realistic altered content must support the required disclosure workflow.
- The user owns the final publishing decision until an explicit permissioned autonomy level is introduced.

## 8. Success criteria

A user should be able to go from a validated idea to a publish-ready MP4 through one traceable workflow, understand what was generated, what sources were used, what it cost, what requires approval and why the system recommends the topic.
