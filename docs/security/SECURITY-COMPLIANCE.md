# Security, Rights and Compliance

## 1. Security principles

- Least privilege.
- Tenant isolation.
- Secrets never stored in source code.
- Encryption in transit and at rest where supported.
- Audit logs for sensitive actions.
- Explicit authorization for channel operations.
- Provider credentials scoped to required permissions.

## 2. Sensitive actions

Require elevated authorization for:
- connecting a channel
- publishing
- deleting content
- changing monetization-related configuration
- spending money
- sending external communications

## 3. Content safety gate

Before publication, evaluate:

- originality
- reused content risk
- repetitive/mass-produced content risk
- factual confidence
- source quality
- rights/licensing
- AI disclosure requirements
- sensitive-topic risk
- advertiser suitability
- spam/deceptive behavior
- title/thumbnail integrity

A failed critical check blocks publication.

## 4. Rights and provenance

Every external/generated asset should have provenance metadata. The system must distinguish:

`licensed | owned | generated | public-domain/verified | unknown`

Unknown rights status should not be treated as safe by default.

## 5. AI transparency

The product must support disclosure workflows when realistic synthetic or altered content falls under applicable platform disclosure requirements.

## 6. High-risk topics

Finance, legal, health and political content require stronger fact checking and human review. The system should not present uncertain AI output as professional advice.

## 7. Privacy

Collect only data necessary for the product. Define retention policies for source material, comments, analytics and generated assets.

## 8. Auditability

For every publication, retain an audit record containing:
- approving user
- content version
- sources
- asset manifest
- QA result
- disclosure decision
- publication timestamp
- external resource ID

## 9. Incident response

Incidents should be classified, logged, contained and reviewed. High-impact automation must have a disable/kill-switch mechanism.
