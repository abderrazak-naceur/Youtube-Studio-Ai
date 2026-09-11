# Cloud Strategy

## Principles

- Cloud should be selected by workload, not ideology.
- Keep application architecture portable.
- External AI providers remain replaceable.
- Object storage uses an S3-compatible abstraction.
- Infrastructure costs are tagged per tenant/project/job.

## 2026

DigitalOcean is the default candidate for simple application infrastructure because pricing is predictable and operational complexity is lower. AWS is introduced where managed services materially reduce engineering effort or improve security/reliability.

## 2027

Use a hybrid approach. Keep stateless application workloads portable and evaluate AWS for enterprise requirements.

## 2028+

AWS can become the primary enterprise cloud while DigitalOcean remains useful for isolated workloads, development, cost-sensitive services or redundancy.

## Required portability

- Docker
- Terraform/OpenTofu
- PostgreSQL
- Redis-compatible cache
- S3-compatible object storage
- OpenTelemetry
- provider adapters
