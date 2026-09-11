# Engineering Backlog 2026

## EPIC 1 — Platform foundation

### US-001 Workspace
Create and persist a workspace.

**Acceptance criteria**
- Workspace has stable ID.
- Data is tenant-scoped.
- Basic settings are persisted.

### US-002 Project lifecycle
Create, update, resume and archive a video project.

**Acceptance criteria**
- Project status is persisted.
- Failed workflows can resume from the last completed stage.

### US-003 Job infrastructure
Implement asynchronous jobs with retries and status tracking.

**Acceptance criteria**
- Jobs have unique IDs.
- Transient failures retry safely.
- Job state is observable.

## EPIC 2 — Channel intelligence

### US-004 Channel profile
Store niche, audience, language and strategic goals.

### US-005 Channel DNA
Create versioned editorial identity.

### US-006 Opportunity scoring
Score ideas using configurable factors.

## EPIC 3 — Research

### US-007 Research workspace
Store sources, notes and claims.

### US-008 Fact checking
Track claim verification and confidence.

### US-009 Content brief
Generate an evidence-backed brief from research.

## EPIC 4 — Script and scenes

### US-010 Script generation
Generate a structured script from an approved brief.

### US-011 Script revision
Support versioning and targeted revisions.

### US-012 Scene planning
Convert script segments into timed production scenes.

## EPIC 5 — Production

### US-013 Asset manifest
Track every required asset and provenance.

### US-014 Voice generation
Generate narration through a provider abstraction.

### US-015 Captions
Generate synchronized subtitles.

### US-016 Timeline
Assemble scenes, audio and assets.

### US-017 Render
Produce preview and final MP4.

### US-018 Thumbnail
Generate thumbnail candidates with metadata.

## EPIC 6 — Quality

### US-019 Technical QA
Validate media integrity and output specifications.

### US-020 Content QA
Validate source confidence, originality and consistency.

### US-021 Publication safety gate
Block critical policy/rights failures.

## EPIC 7 — Economics and learning

### US-022 Cost tracking
Track provider and rendering costs.

### US-023 Content Genome
Extract structured attributes from completed content.

### US-024 Outcome tracking
Store performance and connect outcomes to content decisions.

## Engineering priorities

**P0:** US-001–003, US-007–012, US-013–017, US-019–021.

**P1:** US-004–006, US-018, US-022–024.

## Delivery rule

Do not start a new major epic while the previous epic has no demonstrable end-to-end increment. Prefer vertical slices over large infrastructure-only milestones.
