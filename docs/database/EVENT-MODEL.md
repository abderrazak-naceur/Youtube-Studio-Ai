# Event Model

## Purpose

Enable asynchronous workflows, auditability and learning without tightly coupling agents.

## Event examples

- `opportunity.created`
- `research.completed`
- `script.generated`
- `asset.generated`
- `render.completed`
- `qa.failed`
- `publication.approved`
- `video.published`
- `analytics.snapshot.created`
- `revenue.recorded`
- `cost.recorded`

## Event envelope

```json
{
  "id": "uuid",
  "type": "asset.generated",
  "workspace_id": "uuid",
  "aggregate_id": "uuid",
  "occurred_at": "UTC timestamp",
  "schema_version": 1,
  "payload": {}
}
```

Events are immutable. Consumers must be idempotent.
