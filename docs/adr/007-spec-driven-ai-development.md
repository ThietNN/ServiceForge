# ADR-007: Spec-driven AI-assisted development

**Status**: Accepted

## Context

ServiceForge uses GitHub Spec Kit and requires traceability from approved requirements through plans,
tasks, implementation, and tests. AI assistance must remain bounded, reviewable, and verifiable.

## Decision

Use GitHub Spec Kit for specifications, plans, and tasks. Codex may be used for bounded implementation
tasks only after explicit authorization. Human review is required for AI-generated changes, including
correctness, architecture, security, and testing. Meaningful AI contributions, corrections, and actual
verification are recorded in the AI development log. Git checkpoints are used before and after bounded
work. Specification approval alone does not authorize implementation.

## Rationale

The process makes approved requirements authoritative, prevents silent scope expansion, and preserves
an auditable record of AI assistance and verification.

## Consequences and tradeoffs

- Documentation and review occur before implementation.
- AI output requires human review and may not be treated as verified without execution.
- Work is bounded into smaller checkpoints and can take longer than unconstrained generation.
- Conflicts or ambiguities stop the workflow until explicitly resolved.

## Alternatives considered

- Untracked AI-driven implementation: rejected because it breaks requirements traceability and review.
- Generating code before approved specifications and tasks: rejected by the constitution.
- Treating AI-generated tests as executed evidence: rejected because tests must actually run.
