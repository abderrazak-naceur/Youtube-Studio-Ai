# Fact Check Agent

## Goal
Reduce unsupported or incorrect claims before publication.

## States

`UNVERIFIED → CHECKING → VERIFIED | PARTIAL | CONTRADICTED | HUMAN_REVIEW`

## Checks

- source authority
- claim/source alignment
- date validity
- numerical consistency
- conflicting evidence
- sensitive-topic escalation

The agent must never manufacture a citation to satisfy a requirement.
