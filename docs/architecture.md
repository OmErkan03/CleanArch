# Architecture

> Filled at bootstrap (2026-11-19) from the existing code and the interview. Describes what is
> decided, not an aspiration. Agents read this file before planning.

## System overview

A single ASP.NET Core Web API (monolith) in Clean Architecture layers with CQRS: every use case is
a command or query sent through the in-house mediator (`Uludag.Core.Mediation`, not MediatR), whose
pipeline behaviors handle authentication, authorization and logging. PostgreSQL (EF Core + Npgsql)
holds the domain data; Redis (master/replica with Sentinel) holds sessions, refresh tokens, MFA
challenges and rate-limit buckets. The layering keeps the domain and use cases independent of the
web framework and the database — the point of the training project. Local infrastructure runs from
`docker-compose.yml`.

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `ECommerce.Domain` | Entities, value objects (`Money`), domain markers (`IOwnedEntity`) | The domain model |
| `ECommerce.Application` | Use cases: `Features/<Entity>/{Commands,Queries,Rules,Mappers,Dtos,Constants}`; repository interfaces (`Services/Repositories`) | Business rules, claim names, user-facing messages |
| `ECommerce.Persistence` | EF Core `UludagDbContext`, configurations, migrations, seeds, repository implementations, ownership filter + `SaveChanges` guard | The PostgreSQL schema and data |
| `ECommerce.WebAPI` | Composition root, controllers, exception → ProblemDetails, JWT setup, rate-limit middleware, `ICurrentUser` | The HTTP contract |
| `Uludag.Core` | Project-independent building blocks: mediator, security (JWT, hashing, encryption, MFA, sessions, refresh tokens, ownership context), caching (Redis), rate limiting, base exceptions | Redis keys for sessions, tokens, challenges, buckets |
| `ECommerce.Infrastructure` | Reserved for adapters to external services (mail, payment, …); empty today, referenced by nobody | — |

## Communication rules

- Controllers only build a request and call `ISender.Send(...)`; no logic, no if-else on outcomes.
- Handlers use repositories through Application interfaces; business rules live in `<Entity>BusinessRules` and throw `Uludag.Core.Exceptions` on violation.
- Cross-feature reactions use mediator notifications (`INotification`, e.g. `UserOperationClaimsChangedNotification`).
- Exceptions travel to `GlobalExceptionHandler`, which maps them to HTTP status codes.

## Forbidden dependencies (make them testable)

- **FD-1:** `ECommerce.Domain` references no other project and no EF Core / ASP.NET Core package.
- **FD-2:** `ECommerce.Application` never references `ECommerce.Persistence`, `ECommerce.WebAPI`, EF Core or ASP.NET Core.
- **FD-3:** `Uludag.Core` never references any `ECommerce.*` project (it stays project-independent).
- **FD-4:** Controllers never use a repository or `UludagDbContext` directly — only `ISender`.
- **FD-5:** No raw SQL API (`FromSqlRaw`, `ExecuteSqlRaw*`, `SqlQueryRaw`) anywhere — enforced by BannedApiAnalyzers (RS0030 as error).

FD-5 is enforced by the build today; FD-1…FD-4 are enforced by review until architecture tests
exist (backlog).

## Deliberately out of scope

- Payments, shipping, stock reservation, order status workflow.
- A UI — the API is the product (Swagger UI in Development only).
- Splitting into services.
