# Security

> Filled at bootstrap (2026-11-19). Baseline rules agents must honor in every plan and review.
> Auth, authorization & ownership and money are sensitive areas (`docs/domain.md`).

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Known gap (bootstrap): `appsettings.Development.json` is tracked and holds the JWT `SecurityKey`
  and database / Redis passwords; `docker-compose.yml` has default dev passwords. Treat these as
  **local-development-only** values, never reuse them anywhere else. Moving them to user-secrets /
  environment variables is backlog item B-1 (high priority). New secrets go to user-secrets or
  environment variables only — never into a tracked `appsettings*.json`.

## Input & output
- Validate at the boundary (request objects); business rules that need state live in `<Entity>BusinessRules`.
- No raw SQL: banned APIs fail the build (RS0030); interpolated `FromSql`/`ExecuteSql` only, which always parameterize.
- Errors: ProblemDetails only. A 500 never carries exception detail or stack traces to the client.
- Never reveal whether a record or account exists: another user's owned record → 404 (BR-4);
  unknown e-mail and wrong password → the same 400 at the same hashing cost (BR-11).
- Identity and ownership fields (`UserId`, totals) are never bound from the request body (BR-6, BR-7);
  route ids win over body ids.

## AuthN / AuthZ
- **Default deny.** Every mediator request is authenticated (`AuthenticationBehavior`); only an
  explicit `IAllowAnonymousRequest` opts out. Every non-anonymous use case implements
  `ISecuredRequest` with its `Roles` (`AuthorizationBehavior`, 403 otherwise; `Admin` passes all).
- JWT: signed, issuer + audience + lifetime validated, `ClockSkew = 0`; it carries only a session
  key — user details and claims are loaded from the Redis session on every request.
- Refresh tokens: rotating, 3 days, HttpOnly cookie, stored in Redis; logout / logout-all revoke them.
- MFA (TOTP) is required on login; secrets are AES-GCM encrypted at rest; codes are single-use per
  time step; 5 failed attempts revoke the challenge.
- Ownership is enforced in the data layer (global query filter + `SaveChanges` guard), not in
  handlers — a handler that forgets a check is still safe. Never bypass it (`docs/conventions.md`, Known traps).
- Rate limiting applies to every request (leaky bucket, per user else per IP); behind a proxy,
  `X-Forwarded-For` is trusted only from configured proxies.

## Dependencies
- A new NuGet package is proposed in the plan (name, version, why, license, maintenance status)
  and approved by a human at the plan gate.
- Check before adding: OSI-approved permissive license, maintained (release in the last 12 months),
  no known vulnerabilities (`dotnet list package --vulnerable --include-transitive`).
- Pin exact versions; keep Microsoft.* packages on the same .NET 10 patch line.
- Security-relevant code (crypto, hashing, tokens) uses platform / well-known libraries — no home-made crypto primitives.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
