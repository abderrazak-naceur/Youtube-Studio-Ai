# Test Strategy

## Layers

1. Unit tests
2. Integration tests
3. Provider contract tests
4. Workflow tests
5. AI evaluation
6. End-to-end tests
7. Load/performance tests
8. Security tests

## AI-specific rule

Do not test AI systems only with exact string equality. Evaluate structured quality dimensions and use deterministic fixtures where possible.

## Production gate

A release requires passing critical tests, no unresolved security blocker and successful migration/rollback validation.
