# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- A criterion moved out at the ship gate (R-08, `→ moved to B-<n>`) lands here too, by the
  human's ship decision; the entry names the spec and criterion.
- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
  A decision deferred at bootstrap (its REPORT) uses Source: bootstrap <YYYY-MM-DD>.
-->

- **B-1** — JWT `SecurityKey` and PostgreSQL / Redis passwords are tracked in `appsettings.Development.json` (and dev defaults in `docker-compose.yml`) — Source: bootstrap 2026-11-19 — Severity: High — Recommendation: move them to user-secrets / environment variables, add `.env.example`, rotate the key; a spec (sensitive area: auth) — Deferred by: omer.erkan, 2026-11-19, bootstrap does not change code
- **B-2** — No `global.json`: the build uses whatever SDK the machine has (here 11.0.100) — Source: bootstrap 2026-11-19 — Severity: Low — Recommendation: pin SDK 10.0.x with `rollForward: latestFeature` and use the same in CI — Deferred by: omer.erkan, 2026-11-19, bootstrap does not change code
- **B-3** — 10 direct `DateTime.UtcNow` call sites (JWT, sessions, refresh tokens, MFA, orders) make time-dependent behavior untestable — Source: bootstrap 2026-11-19 — Severity: Medium — Recommendation: inject `TimeProvider`; best done with or right after spec 0001 — Deferred by: omer.erkan, 2026-11-19, bootstrap does not change code
- **B-4** — Forbidden dependencies FD-1…FD-4 (`docs/architecture.md`) are enforced only by review — Source: bootstrap 2026-11-19 — Severity: Medium — Recommendation: architecture tests (e.g. NetArchTest) in the unit test project — Deferred by: omer.erkan, 2026-11-19, needs the test skeleton first
- **B-5** — Inline user-facing messages outside `<Feature>Constants`, some in English (e.g. "Product name already exists.", "Sipariş bulunamadı.") — Source: bootstrap 2026-11-19 — Severity: Low — Recommendation: move them to Constants in Turkish (`docs/conventions.md`, Error handling) — Deferred by: omer.erkan, 2026-11-19, bootstrap does not change code
- **B-6** — 5 nullable warnings (CS8618 on `User`/`Product`, CS8777 in `MfaBusinessRules`); warnings are not errors — Source: bootstrap 2026-11-19 — Severity: Low — Recommendation: fix them, then propose `TreatWarningsAsErrors` in `Directory.Build.props` — Deferred by: omer.erkan, 2026-11-19, adoption: build flags are proposed, not imposed
