# Conventions

> Filled at bootstrap (2026-11-19). Every rule here is enforced by tooling or checked in review.

## Language
- Chat language (interviews, sessions): Turkish (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): English (`en`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: English. Code comments may be Turkish (training
  project). User-facing messages: Turkish (see Error handling).
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- C# / .NET 10 (`net10.0` in every project); ASP.NET Core 10; EF Core 10.0.x + Npgsql 10.0.x.
- PostgreSQL 17, Redis 7 (Sentinel), StackExchange.Redis 3.x, Otp.NET, Swashbuckle UI.
- SDK not pinned yet (this machine builds with SDK 11.0.100) — `global.json` is a backlog item.
- Nullable and implicit usings enabled in every project.

## Naming
- Folders: `Features/<Entity>/Commands/<Action>/<Action><Entity>Command.cs`,
  `Features/<Entity>/Queries/<Action>/<Action><Entity>Query.cs`; the handler is a nested class of
  its request (`CreateOrderCommand.CreateOrderCommandHandler`).
- Rules: `<Entity>BusinessRules : BaseBusinessRules`, methods named as the rule
  (`ProductNameCannotExist`, `OrderShouldExistWhenSelected`).
- Mappers: hand-written static `<Entity>Mapper` classes (no AutoMapper). DTOs: `<Entity>Response` records.
- Claims: `<Entity>OperationClaims` constants (`docs/domain.md` BR-3). Messages: `<Feature>Constants`.
- Branches and commits: `docs/git.md`.

## Error handling
- Business rule violations throw `Uludag.Core.Exceptions`: `BusinessException` (400),
  `UnauthorizedException` (401), `ForbiddenException` (403), `NotFoundException` (404).
  `GlobalExceptionHandler` maps them to ProblemDetails (RFC 7807). No try/catch or status if-else
  in controllers or handlers.
- Unexpected exceptions → 500 with no detail to the client; logged server-side only.
- User-facing messages are **Turkish** and come from `<Feature>Constants`; new code writes no
  inline message strings. Existing inline ones are a backlog item.

## Data rules
- IDs are `Guid` (except `OperationClaim.Id`, a seeded `int`).
- Money is always the `Money` value object (decimal amount + currency, default `TRY`), never a bare decimal.
- Timestamps are UTC (`timestamptz`). New code takes time from `TimeProvider`, not `DateTime.UtcNow`.
- Every async method takes and passes a `CancellationToken`.
- Read queries use `AsNoTracking()` in repositories.
- Ownership: owned entities implement `IOwnedEntity`; `UserId` comes from `ICurrentUser`.
- No raw SQL (see Enforced by tooling); use `FromSql($"...")` / `ExecuteSql($"...")` interpolation.

## Known traps
- Disabling all query filters with `IgnoreQueryFilters()` drops the ownership (IDOR) filter — disable only a named one: `IgnoreQueryFilters([OwnershipFilter])`, and only with a stated reason — from bootstrap.
- `DateTime.UtcNow` makes token, TOTP and rate-limit code untestable — use `TimeProvider` (tests use `FakeTimeProvider`); 10 existing call sites are a backlog item — from bootstrap.
- Npgsql `timestamptz` accepts only `DateTimeKind.Utc` values — create UTC values, never `DateTime.Now` — from bootstrap.
- A `BannedSymbols.txt` entry without the full overload signature bans nothing — write every overload with its full documentation-comment ID (from `Microsoft.EntityFrameworkCore.Relational.xml`) — from bootstrap.
- `Money` is a value object — replace it (`new Money(...)`), never set its fields; after any item change call `Order.RecalculateTotalPrice()` — from bootstrap.
- MTP `dotnet test` emits ANSI colour codes in CI even when its output is captured — strip them before parsing its summary (`ECommerce/tests/run-tests.sh`) — from 0001 review round 1, F-1.
- The app reaches Redis through Sentinel at `127.0.0.1` (shared network namespace in `docker-compose.yml`) — changing `network_mode` or announce IPs breaks failover — from bootstrap.

## Enforced by tooling
- Raw SQL APIs are banned solution-wide: `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt`, RS0030 as error (`Directory.Build.props`) — the build fails.
- Nullable reference types are on (warnings today, not errors — backlog).
- `./scripts/check` builds the whole solution (`scripts/check.conf`).
