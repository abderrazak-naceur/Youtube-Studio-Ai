# Youtube Studio AI — Documentation Hub

## Purpose
This directory is the single documentation system for Youtube Studio AI as a **software company + AI media company**.

The product is an AI-native operating system for building profitable, original and sustainable YouTube businesses. The core loop is:

**FIND → RESEARCH → CREATE → PRODUCE → PUBLISH → MEASURE → LEARN → MONETIZE → SCALE**

The primary optimization target is **net business value per published video**, not raw upload volume.

## Documentation map

- `study/` — product and market study
- `business-plan/` — 2026–2030 strategy, pricing and funding
- `company/` — organization, roles and operating model
- `finance/` — costs, OPEX, cash flow and break-even
- `cloud/` — AWS, DigitalOcean, architecture, scaling and cloud costs
- `product/` — requirements and product rules
- `architecture/` — system and AI-agent architecture
- `data/` — domain model, schema and events
- `api/` — API, authentication and endpoints
- `ai/` — model routing, provider strategy and AI economics
- `agents/` — individual AI-agent specifications
- `providers/` — concrete provider integrations
- `content/` — research, scripts, scenes and Content Genome
- `production/` — media generation and rendering
- `youtube/` — YouTube integration and publishing
- `analytics/` — performance intelligence and experimentation
- `revenue/` — monetization, profitability and AI CFO
- `security/` — security, permissions, rights and compliance
- `ux-ui/` — information architecture, screens, flows and design system
- `workflows/` — end-to-end business and technical workflows
- `testing/` — engineering and AI evaluation strategy
- `roadmap/` — implementation phases and milestones
- `implementation/` — executable engineering backlog
- `operations/` — quality gates and operating procedures

## Company architecture

```text
                 YOUTUBE STUDIO AI COMPANY
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
     SOFTWARE          AI/INTELLIGENCE     OWNED MEDIA
        │                  │                  │
      SaaS            Model Router       Channels
      APIs             Agents            Content
      Enterprise       Data              Revenue
        │                  │                  │
        └──────────────────┼──────────────────┘
                           ↓
                    PROFITABLE MEDIA OS
```

## Cloud strategy

The default 2026 strategy is **DigitalOcean-first + external AI providers**, with AWS introduced when managed services, enterprise requirements, security, global scale or economics justify the added complexity.

The architecture must remain portable so the company can use both AWS and DigitalOcean without rewriting the product.

## AI provider strategy

AI providers are adapters behind a Model Router. Initial targets include:

- Higgsfield for premium visual/video generation
- Hugging Face for model/provider access and open-model experimentation
- direct model APIs
- AWS Bedrock where it improves enterprise architecture

Every provider call records usage and cost so customer and video profitability can be calculated.

## UX/UI strategy

The application is designed as a professional operating system, not a collection of chat screens. The UI exposes the business loop, explains AI recommendations, shows cost before expensive actions and keeps sensitive actions permissioned.

## Source of truth hierarchy

1. Product and legal constraints
2. Business strategy and financial constraints
3. Architecture decisions
4. Product requirements
5. Implementation backlog
6. Code
7. Experiments and hypotheses

When documents conflict, update the higher-level source of truth first and reconcile downstream documents.

## 2026 priority

The first objective is not to build the entire 2030 vision. The first objective is to prove a repeatable production engine:

`idea → research → fact check → script → scene plan → assets → voice → edit → captions → thumbnail → metadata → quality gate → MP4`

The MVP must produce useful, original content at predictable cost and leave structured data behind so every published result improves future decisions.

## Core principles

- Originality over volume.
- Quality over automation for its own sake.
- Human approval for high-impact actions during early phases.
- Every important AI decision should be explainable and traceable.
- Every generated asset must have provenance and rights information.
- Costs must be measurable per provider, asset, video and customer.
- Revenue must eventually be connected to content decisions.
- Cloud and AI vendors remain replaceable.
- Automation must never become fake engagement, spam or mass-produced low-value content.
