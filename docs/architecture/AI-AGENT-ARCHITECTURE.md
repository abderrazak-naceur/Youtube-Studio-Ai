# AI Agent Architecture

## 1. Philosophy

Agents are specialized decision-makers, not unrestricted autonomous bots. Each agent has a narrow responsibility, explicit inputs/outputs, permissions and auditability.

## 2. Agent map

### Strategist / AI CEO
Chooses priorities across channels and workflows based on expected business value.

### Opportunity Agent
Finds topics and ranks them using demand, competition, audience fit, monetization and production feasibility.

### Research Agent
Collects and structures evidence from approved sources.

### Fact Checker
Tests claims against evidence and marks uncertainty.

### Script Agent
Transforms research into an original narrative aligned with Channel DNA.

### Producer Agent
Converts script into scenes, assets, timing and production instructions.

### QA Agent
Checks originality, factual confidence, rights, metadata integrity and policy-sensitive conditions.

### Analytics Agent
Explains performance and identifies patterns.

### Revenue Agent / AI CFO
Estimates profitability, revenue opportunities and resource allocation.

## 3. Common agent contract

Each agent should receive:
- task ID
- context ID
- user/channel permissions
- relevant source data
- constraints
- budget
- expected output schema

Each agent returns:
- structured result
- confidence
- reasoning summary suitable for audit (not hidden chain-of-thought)
- sources/artifacts used
- warnings
- recommended next action

## 4. Model Router

The router selects a provider/model based on:
- task type
- quality requirement
- latency requirement
- cost budget
- context length
- structured output support
- availability

Do not hard-code the product to a single model provider.

## 5. Autonomy levels

### Level 0 — Assistant
Suggests actions only.

### Level 1 — Drafting
Creates drafts but requires approval.

### Level 2 — Workflow execution
Runs approved production workflows but does not publish without approval.

### Level 3 — Permissioned operations
Can publish within explicitly configured constraints.

### Level 4 — Bounded autonomous creator
Can discover, produce and operate within budgets, policies, approval thresholds and rollback rules.

Level 4 is a future state, not an MVP requirement.

## 6. Safety boundary

Agents must not:
- fabricate evidence
- manipulate engagement
- bypass platform restrictions
- publish sensitive content without required review
- spend money outside configured limits
- access channels without authorization

## 7. Learning loop

Every completed workflow should update:

`Outcome → Feature extraction → Content Genome → Opportunity Graph → Future recommendation`

The system should learn from both successes and failures.
