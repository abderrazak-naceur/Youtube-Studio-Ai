# AWS Architecture

## Target architecture

```text
CloudFront
   ↓
Load Balancer / API
   ↓
ECS or EKS workloads
   ├── API
   ├── Workers
   └── Scheduler

RDS PostgreSQL ── ElastiCache
        │
        └── S3 media assets

SQS/EventBridge → async production jobs

Bedrock / external AI providers → AI Router

CloudWatch + OpenTelemetry → observability
```

## When AWS is justified

- enterprise customers
- advanced IAM and audit requirements
- multi-region
- large predictable workloads
- managed event infrastructure
- dedicated security/compliance requirements

## Cost controls

- budgets and alerts
- tagging by environment/tenant
- autoscaling
- lifecycle policies for media
- reserved/Savings Plan analysis for stable compute
- spot capacity for interruptible rendering where appropriate

AWS pricing is primarily pay-as-you-go and AWS provides a pricing calculator and Savings Plans. Validate current prices before committing budget. citeturn0search2
