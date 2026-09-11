# Video Workflow

```text
Opportunity
 → Research
 → Fact Check
 → Brief
 → Script
 → Scene Plan
 → Asset Generation
 → Voice
 → Timeline
 → Render
 → Technical QA
 → Editorial QA
 → Publication Gate
 → Publish
 → Analytics
 → Learning
```

Each transition is persisted as state and emits an event. Failed steps are retryable where safe and must not duplicate paid external operations without idempotency controls.
