# AI Provider Integrations

## Goal

Integrate AI providers behind a common internal contract so the product can switch providers without changing business workflows.

## Initial providers

- OpenAI / direct APIs
- Hugging Face Inference Providers
- Higgsfield API
- AWS Bedrock
- future providers through adapters

## Provider interface

Each adapter should expose:

- capabilities
- model catalog
- authentication
- generation request
- status polling
- cancellation where supported
- output normalization
- usage/cost metadata
- retry policy
- safety metadata

## Hugging Face

Hugging Face Inference Providers gives unified access to hundreds of models/providers with pay-as-you-go billing and no Hugging Face markup on routed provider rates. It supports custom provider keys and organization billing. citeturn0search0turn0search6

Use cases:

- open models
- embeddings
- classification
- image/video experimentation
- provider comparison
- dedicated Inference Endpoints when justified

## Higgsfield

Higgsfield exposes an official API platform and documentation and also an MCP integration. The official Node/TypeScript SDK is server-side only, which is appropriate for our backend architecture. citeturn1search0turn1search1

Use cases:

- premium image/video generation
- cinematic scenes
- character/visual workflows
- automated production

Important: automated/API/MCP usage consumes credits according to the applicable Higgsfield plan; unlimited web-only benefits must not be assumed for API workloads. citeturn1search7turn1search4

## Architecture

```text
Agent → Model Router → Provider Adapter → External API
                         ↓
                  Cost/Usage Ledger
                         ↓
                    Asset Registry
```
