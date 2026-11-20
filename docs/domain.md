# Domain

> The shared language between business, humans, and agents. Filled at bootstrap (2026-11-19) from
> the existing code: it documents what the code does today, it does not add rules.

UludagEnerji is a training project: a Clean Architecture e-commerce Web API (products, orders,
order items, users and operation claims) used to teach and practice production-grade security
patterns — JWT with Redis-backed sessions, refresh tokens, MFA, IDOR protection, rate limiting and
banned raw SQL.

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Product | A sellable item with a `Price` (Money), a unique `Name` and a `Stock` count | `Stock` is stored only; ordering does not decrement it today |
| Order | A user's order; created empty, its `TotalPrice` is the sum of its items | Owned entity; `OrderDate` set by the server (UTC) |
| OrderItem | One product line in an order: `Quantity` × product price = item `TotalPrice` | Item total is computed at add/update time from the product's price then |
| User | An account: e-mail, password hash + salt, MFA state, operation claims, orders | Not an owned entity; managed by Auth and admin endpoints |
| OperationClaim | A named permission (`Product.Create`, `Order.Read`, `Admin`, …); many-to-many with User | Checked by **name** (text), names are unique |
| Money | Value object: `Amount` (decimal, ≥ 0) + `Currency` (default `TRY`) | Immutable in use: replace, never mutate fields |
| Owned entity | An entity implementing `IOwnedEntity` (has `UserId`); today: Order | Its owner is the session user, never the client |
| Session | Redis-stored `UserSession` (user id, e-mail, claims); the JWT carries only its key | Revoked on logout; claim changes update it immediately |
| Refresh token | Rotating 3-day token in an HttpOnly cookie, stored in Redis | Rotation: each use issues a new one |
| MFA challenge | Short-lived (5 min) Redis record for a half-finished login (`Setup` or `Verify` purpose) | Its token is not a JWT |
| `<Entity>.Admin` / `Admin` | Claims that bypass ownership for one entity / for everything | `Admin` is accepted by every secured request |

## Business rules

- **BR-1:** Every request is authenticated unless it implements `IAllowAnonymousRequest` (secure by default).
- **BR-2:** A request implementing `ISecuredRequest` succeeds only if the user holds at least one of its `Roles`, or `Admin`; otherwise 403.
- **BR-3:** Claim names per entity: `<Entity>.Admin`, `.Read`, `.Write`, `.Create`, `.Update`, `.Delete`; `Write` covers Create/Update/Delete, `Read` covers GetById/GetAll. A new claim constant is also seeded (fixed Id) with a migration.
- **BR-4:** A user sees only their own owned entities; another user's record reads as not found (404), its existence is never revealed. `<Entity>.Admin` / `Admin` see all.
- **BR-5:** Another user's owned record cannot be added, updated, deleted, or re-owned (403), enforced in `SaveChanges`.
- **BR-6:** An order belongs to the session user who created it; `UserId` is never taken from the client.
- **BR-7:** An order's `TotalPrice` is the sum of its items' totals, recalculated on every item add / update / delete; never taken from the client.
- **BR-8:** A product added to an order must have the order's currency (400 otherwise).
- **BR-9:** Product names are unique (400 on duplicate).
- **BR-10:** E-mail addresses are unique at registration (400 on duplicate).
- **BR-11:** Login with an unknown e-mail or a wrong password returns the same 400 "invalid credentials", with the same hashing cost (no user enumeration, no timing leak).
- **BR-12:** Login requires TOTP MFA: without MFA set up, login returns a setup challenge instead of tokens; with MFA, a verify challenge.
- **BR-13:** An MFA challenge token is valid only for its purpose (setup ≠ verify), for 5 minutes, and only while the user's MFA state still matches it; otherwise 401.
- **BR-14:** A TOTP code is accepted once per 30 s time step (no replay); after 5 failed attempts the challenge is revoked (401).
- **BR-15:** An admin can reset a user's MFA (lost device); a user without MFA enabled cannot be reset (400).
- **BR-16:** An operation claim cannot be assigned twice, nor revoked if not held (400); unknown user / claim → 404. Changes reach active sessions immediately.
- **BR-17:** Logout revokes the current session; logout-all revokes every session, refresh token and pending MFA token of the user.
- **BR-18:** Every API request is rate limited by a leaky bucket per user (else per IP): capacity 10, one request leaks every 12 s; over the limit → 429.
- **BR-19:** Money amounts are never negative.
- **BR-20:** A product name is required (not empty or whitespace) and at most 100 characters (spec 0002).
- **BR-21:** A product's stock is never negative (spec 0002).
- **BR-22:** A product's currency is a 3-letter code (spec 0002).
- **BR-23:** A product amount has at most 2 decimal places (spec 0002).
- A violation of BR-9 or BR-19…BR-23 is a client error: 400 with a ProblemDetails body, and nothing is stored.

## Key domain invariants

- Ownership of an owned entity always comes from the session, never from request data (BR-6).
- An order's total always equals the sum of its items (BR-7), all in one currency (BR-8).
- No endpoint is anonymous by accident (BR-1); no raw SQL string reaches the database (`docs/security.md`).

## Sensitive areas

A spec touching one says so in Constraints (`Sensitive area: <name>`).

- **auth** — authentication, sessions, JWT, refresh tokens, MFA, password hashing, encryption helpers (`Uludag.Core/Security`, `Features/Auth`, `WebAPI/Security`).
- **authorization & ownership** — operation claims, `AuthorizationBehavior`, `IOwnedEntity` filter and `SaveChanges` guard (`UludagDbContext.Ownership.cs`), rate limiting.
- **money** — `Money`, order and item totals, currency rules (BR-7, BR-8, BR-19).
