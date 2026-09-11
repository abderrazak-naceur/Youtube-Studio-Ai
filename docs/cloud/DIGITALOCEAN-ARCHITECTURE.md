# DigitalOcean Architecture

## 2026 reference stack

```text
Cloud Firewall
    ↓
Load Balancer
    ↓
DOKS / Droplets
 ├── Web/API
 ├── Workers
 └── Scheduler

Managed PostgreSQL
Redis/Valkey
Spaces Object Storage
Monitoring
```

DigitalOcean currently provides DOKS with managed control plane, autoscaling and node pools billed from Droplet pricing. Basic Kubernetes nodes start at $12/month/node according to the current pricing page. citeturn0search4turn0search8

Spaces starts at $5/month and includes 250 GiB under the standard storage subscription. citeturn0search10

## Best use

- MVP
- early SaaS
- predictable infrastructure
- development/staging
- isolated media workloads

## Limits to evaluate

- enterprise integrations
- global footprint
- advanced managed services
- compliance requirements
- GPU availability/cost for proprietary inference

AI inference should normally remain with specialized providers until utilization justifies dedicated GPU infrastructure.
