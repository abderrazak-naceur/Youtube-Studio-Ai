# Higgsfield Integration

## Role

Premium visual/video generation provider for production scenes, B-roll, characters and cinematic assets.

## Integration

Use the official server-side API/SDK from the backend adapter. Higgsfield confirms its official API platform and API documentation, and its official Node/TypeScript SDK is server-side only. citeturn1search0turn1search1

## Architecture

```text
Producer Agent
  ↓
Model Router
  ↓
HiggsfieldAdapter
  ↓
Higgsfield API
  ↓
status/polling
  ↓
Asset Registry
```

## Commercial rule

Automated/API/MCP generation consumes applicable credits. The product must model actual API credit consumption and never assume unlimited web-plan benefits apply to automated calls. citeturn1search7turn1search4

## Security

Credentials are server-side secrets. Never expose them in browser code.
