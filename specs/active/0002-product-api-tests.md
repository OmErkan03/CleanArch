# Spec 0002 — Product API tests

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: done

## Intent
The team wants every Product flow in the system proven through the public HTTP API, against real
PostgreSQL and Redis and through the real pipeline (JWT, MFA, Redis session, authorization, rate
limiting), with the coverage of the Product code reported. The tests follow a **"do not trust the
code"** approach: expected behavior comes from the documented rules (`docs/domain.md`,
`docs/security.md`) and the HTTP contract, never from what the code happens to do; reading the
code only maps the surface to test. Where the code deviates, the deviation is surfaced with
evidence, not absorbed into the test. Deliberately not done: unit tests, and any change to
production code — deviations become bug entries for a later fix workflow.

Clarify decisions (human, 2026-11-20): deviations become evidenced backlog bug entries, not red or
skipped tests; API tests run in-memory via `WebApplicationFactory` on real containers (no separate
server process, no UI e2e); authentication uses the real register → login → MFA flow, with the
first admin seeded by one database row (arrange only) and other claims granted through the real
claim API; the test environment raises the rate-limit capacity (test configuration only); the
rules the docs were silent on are written down as BR-20…BR-23 before testing; coverage is measured
with coverlet and reported, not gated; the concurrent-create case is in scope.

## Requirements
- R-1: Every Product endpoint (list, get by id, create, update, delete) is exercised through HTTP
  with an authenticated user obtained through the real authentication flow.
- R-2: The authorization rules (BR-1, BR-2, BR-3) are proven per endpoint: who is accepted, who is
  refused, and that a refused request changes nothing.
- R-3: The happy path of each operation is proven by its observable result (status, headers,
  body, and the state a following read returns), not by status codes alone.
- R-4: The data rules for products — BR-9, BR-19 and the new BR-20…BR-23 — and the error contract
  (`docs/security.md`: ProblemDetails, no exception detail on 500) are checked for each operation
  that accepts product data.
- R-5: Referential integrity (a product used by an order) and concurrent creation with the same
  name are checked.
- R-6: Each expected behavior the code does not meet is recorded once in `specs/backlog.md` as a
  bug entry with the request, the expected and the observed response; no test is committed red,
  skipped or bent to the observed behavior.
- R-7: The line and branch coverage of the Product code and of the whole solution, produced by the
  integration run, is reported at VERIFY.
- R-8: Tests are independent of each other and of execution order; production code is unchanged.

## User experience
none — no user interface. The "users" are API clients; the HTTP contract is the experience.

## Constraints & out of scope
- Sensitive area: money (Product price is `Money`) and authorization & ownership (operation claims).
- New business rules added to `docs/domain.md` by this spec (decided at clarify):
  BR-20 product name is required (not empty or whitespace) and at most 100 characters;
  BR-21 product stock is never negative; BR-22 product currency is a 3-letter code;
  BR-23 product amount has at most 2 decimal places. A violation is a client error (400).
- Expected status for a client error that the rules forbid is 400 with a ProblemDetails body; a
  missing product is 404; a refused claim is 403; no token is 401.
- Tests live in `ECommerce/tests/ECommerce.IntegrationTests` (full profile); no unit tests.
- New packages follow `docs/security.md` "Dependencies".
- Coverage output is shown in the conversation and never stored in the repo (Evidence policy).
- Authorization matrix (AC-2): each accepted claim of an endpoint is tried once; one neighboring
  refused claim per endpoint — `Product.Read` for create/update/delete, `Product.Create` for
  update/delete, `Order.Admin` for list/get. The plan writes the matrix as a table.
- Name uniqueness (BR-9) is tested as an exact match only; case-insensitive duplicates are out of scope.
- The seeded product ("Test Product") exists in every database: list assertions check that the
  products a test added are present, never the exact list (`docs/testing.md`, Lists that grow).
- "Changes nothing" is proven by reading the product back (get) after each refused request.
- Concurrency (AC-9) is race-dependent: its evidence is an observation outside check — 20 rounds
  of two parallel creates with a fresh name each; a single round producing two products is a
  deviation (bug entry). A test for it enters check only if every round behaves as expected.
- Split: proposed 0002 (identity, authorization, CRUD, coverage, isolation) and 0003 (data rules,
  integrity, concurrency, deviation ledger); kept as one — omer.erkan, 2026-11-20, one suite on one harness.
- Out of scope: Order / OrderItem flows beyond what referential integrity needs; rate-limit
  behavior; fixing any deviation; performance.

## Acceptance criteria
- [ ] AC-1 — Anonymous calls to each of the five Product endpoints return 401 with a ProblemDetails body (BR-1).
- [ ] AC-2 — Authorization matrix (BR-2, BR-3): per endpoint, a user holding only an accepted claim succeeds (Read → list/get; Create or Write → create; Update or Write → update; Delete or Write → delete; Product.Admin and Admin → all), and an authenticated user holding only a non-accepted claim gets 403 ProblemDetails with the data unchanged.
- [ ] AC-3 — Create: 201, the body is the new id, `Location` addresses it, and a following get returns exactly the submitted name, amount, currency and stock.
- [ ] AC-4 — Read: the list contains the created products; get by an unknown id returns 404 ProblemDetails.
- [ ] AC-5 — Update: 204 and a following get returns the new values; an id in the body is ignored in favor of the route id; an unknown id returns 404 and creates nothing.
- [ ] AC-6 — Delete: 204, then get returns 404; deleting again or deleting an unknown id returns 404.
- [ ] AC-7 — Data rules on create and update: duplicate name (BR-9), negative amount (BR-19), empty or over-long name (BR-20), negative stock (BR-21), invalid currency (BR-22), more than 2 decimals (BR-23) and malformed JSON each return 400 ProblemDetails and change nothing; zero amount and zero stock are accepted.
- [ ] AC-8 — Deleting a product referenced by an order item returns a 4xx ProblemDetails (not 500) and the product still exists.
- [ ] AC-9 — Two concurrent creates with the same new name: exactly one returns 201, the other 400, and one product with that name exists — observed over 20 rounds outside check (see Constraints).
- [ ] AC-10 — Any 500 observed in this suite carries no exception detail (security.md).
- [ ] AC-11 — Every expectation of AC-1…AC-10 is either proven by a passing test in check, or recorded in `specs/backlog.md` as a bug entry (request, expected, observed, criterion) — none dropped, none committed red or skipped.
- [ ] AC-12 — Coverage of the Product code (controller, feature, mapper, rules, repository, configuration) and of the whole solution — line and branch — is reported from the integration run.
- [ ] AC-13 — The suite passes in any order and on a repeated run (unique data per test, no reliance on other tests), and the residue test stays clean.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
