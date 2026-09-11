# Hugging Face Integration

## Role

Unified access to open models and multiple inference providers for experimentation and production routing.

Hugging Face Inference Providers currently exposes hundreds of models/providers with pay-as-you-go billing; routed usage is billed through Hugging Face at provider rates without an additional HF markup. citeturn0search0turn0search6

## Use cases

- LLM experimentation
- embeddings
- classification
- image generation
- video generation where supported
- provider comparison
- dedicated Inference Endpoints

## Dedicated models

Use Inference Endpoints when a model requires predictable dedicated capacity. Endpoints are billed by running compute time and can scale by replica count. citeturn0search3

## Architecture

```text
Agent → Model Router → HuggingFaceAdapter → Inference Provider
                                      ↘ Dedicated Endpoint
```

## Cost controls

- organization billing
- provider allowlist
- monthly budget
- per-workspace quota
- cost ledger
- automatic fallback
