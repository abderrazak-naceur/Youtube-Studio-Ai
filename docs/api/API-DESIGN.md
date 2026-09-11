# API Design

## Style

REST API for public/product operations; asynchronous jobs for long-running AI/media tasks.

## Principles

- versioned `/api/v1`
- tenant/workspace isolation
- idempotency for expensive mutations
- cursor pagination
- structured errors
- request IDs
- audit events for sensitive actions

## Long-running pattern

`POST /jobs` → `202 Accepted` → job status/events → result resource.

## Authentication

OAuth/OIDC for users; service credentials for internal workers; provider secrets never exposed to browsers.
