# Definition of Done

## Product feature

A feature is done when:
- requirements are documented
- acceptance criteria are testable
- permissions are defined
- failure states are handled
- observability exists
- data is persisted correctly
- documentation is updated

## AI feature

In addition:
- input/output schema is explicit
- prompt/version is traceable
- provider/model is recorded
- confidence/uncertainty is represented where relevant
- cost is measurable
- unsafe or unsupported output has a fallback
- evaluation examples exist

## Media feature

In addition:
- asset provenance is stored
- supported formats are documented
- rendering is reproducible
- output quality checks exist
- failed stages can be retried independently

## Publishing feature

In addition:
- authorization is explicit
- duplicate publication is prevented
- audit record is created
- rollback/recovery procedure exists where applicable

## Release checklist

- Tests pass.
- No secrets committed.
- Migration reviewed.
- Logs are useful but do not leak sensitive data.
- Cost impact understood.
- External provider limits considered.
- Documentation updated.
- Manual smoke test completed.
- Critical safety gates verified.

## MVP quality bar

A video is not considered successfully produced merely because an MP4 exists. It must also have:
- coherent narrative
- valid audio/video
- synchronized captions
- traceable assets
- validated material claims
- acceptable originality
- complete metadata
- recorded production cost
- completed quality gate
