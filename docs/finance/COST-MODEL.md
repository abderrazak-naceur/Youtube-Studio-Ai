# Company Cost Model

## Cost layers

### Fixed
- salaries/contractors
- accounting/legal
- insurance
- SaaS tooling
- domains and corporate services

### Infrastructure
- compute
- database
- object storage
- CDN/network
- queues/cache
- observability
- backups

### AI variable costs
- LLM tokens
- embeddings
- TTS
- image generation
- video generation
- transcription
- moderation

### Media variable costs
- music/SFX licenses
- stock assets
- editing/review
- thumbnails

## Unit cost formulas

`CostPerVideo = AI + Compute + Storage + Rendering + HumanReview + LicensedAssets`

`GrossMargin = Revenue - VariableCost`

`ContributionMargin = Revenue - VariableCost - DirectOperations`

`CACPaybackMonths = CAC / MonthlyGrossProfitPerCustomer`

## Cost allocation

Every job must carry:

- workspace_id
- project_id
- video_id
- provider
- model
- usage quantity
- unit price
- currency
- timestamp

This enables exact customer and video profitability.
