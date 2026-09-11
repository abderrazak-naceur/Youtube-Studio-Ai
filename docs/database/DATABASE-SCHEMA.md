# Database Schema

## Primary PostgreSQL domains

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

## Rules

All tenant-owned tables carry `workspace_id` either directly or through a strongly constrained relationship.

Use UUID identifiers, UTC timestamps, soft deletion where required, immutable audit records and database constraints for state transitions.

Large media binaries stay in object storage; PostgreSQL stores metadata and references.
