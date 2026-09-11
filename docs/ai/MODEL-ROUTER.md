# Model Router

## Purpose

Select the best provider/model for each job based on quality, cost, latency, availability, customer plan and rights/safety constraints.

## Inputs

- task type
- quality target
- budget
- deadline
- output format
- customer tier
- provider availability
- historical quality
- historical failure rate

## Decision score

`Score = QualityWeight × Quality + CostWeight × CostEfficiency + LatencyWeight × Latency + ReliabilityWeight × Reliability`

Hard constraints always run before scoring.

## Example

```text
SCRIPT → LLM router
IMAGE → image router
VIDEO → Higgsfield / video provider router
TTS → speech router
EMBEDDING → embedding router
```

## Learning

Store actual outcome and cost for every request. Periodically recalibrate routing using measured performance rather than vendor marketing claims.

## Safety

The router cannot bypass content QA, rights checks, publication gates or customer budget limits.
