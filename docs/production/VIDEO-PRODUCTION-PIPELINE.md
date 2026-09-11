# Video Production Pipeline

## 1. Goal

Produce a deterministic, inspectable and resumable video from a versioned script and scene plan.

## 2. Pipeline

1. Validate script.
2. Build scene plan.
3. Resolve required assets.
4. Generate or acquire media.
5. Normalize media formats.
6. Generate voice tracks.
7. Align narration and scenes.
8. Add music/SFX where appropriate.
9. Build captions.
10. Assemble timeline.
11. Render preview.
12. Run automated QA.
13. Render final MP4.
14. Store manifest and provenance.

## 3. Scene contract

Each scene should define:
- scene ID
- script segment
- narration text
- duration target
- visual type
- required assets
- transition
- captions
- music/SFX intent
- generation constraints

## 4. Asset manifest

Every asset should retain:
- asset ID
- type
- source/provider
- source URL or reference where appropriate
- license/provenance status
- generation model/tool
- prompt reference when useful
- dimensions/duration
- hash
- usage status

## 5. Rendering

Rendering should be reproducible from:

`script version + scene plan version + asset manifest + audio + timeline settings`

A render must have a unique version and configuration snapshot.

## 6. Quality checks

Automated checks should cover:
- missing media
- broken media
- audio clipping
- silence gaps
- subtitle timing
- frame/duration consistency
- output codec/container
- resolution/aspect ratio
- black frames
- asset licensing status
- required disclosures

## 7. Cost accounting

Record generation cost per:
- LLM call
- image generation
- video generation
- TTS
- transcription
- rendering
- storage
- external API usage

Aggregate these into cost per scene and cost per published video.

## 8. Failure handling

Production must support retrying a failed stage without rebuilding successful stages unnecessarily.

Example: if thumbnail generation fails, the system must not regenerate the entire video.
