# ADR-002: Clean Architecture with CQRS and MediatR

**Status**: Accepted

## Context

The system must keep booking business rules independent from HTTP and SQL Server concerns while
supporting commands, queries, validation, and testable boundaries.

## Decision

Use four Clean Architecture projects: `ServiceForge.Api`, `ServiceForge.Application`,
`ServiceForge.Domain`, and `ServiceForge.Infrastructure`. Use CQRS with MediatR, built-in dependency
injection, FluentValidation, EF Core DbContext as Unit of Work, and focused repository abstractions
where justified. Target `net10.0` and use EF Core Code First with reviewed migrations.

Dependencies point inward: Api composes Application and Infrastructure; Infrastructure implements
Application abstractions and references Domain; Application references Domain; Domain references neither
Infrastructure nor Presentation.

## Rationale

This separates domain invariants, use-case orchestration, SQL Server persistence, and HTTP concerns.
It supports deterministic unit tests and real SQL Server integration tests without making the Domain
depend on infrastructure.

## Consequences and tradeoffs

- Four projects add boundaries and project-reference discipline.
- MediatR and FluentValidation add approved dependencies but keep command/query and validation concerns
  explicit.
- Focused repositories are allowed where justified; an automatic generic repository is not introduced.
- EF Core migrations require review before use.

## Alternatives considered

- Single project: rejected because it weakens the approved Clean Architecture boundaries.
- Generic repository for every entity: rejected because the constitution calls for focused interfaces
  where justified.
- Microservices: rejected because microservices and message queues are excluded from P0.
