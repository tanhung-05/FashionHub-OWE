# API-First Security Review

Review date: 2026-07-30

## Controls Implemented

- Cookie authentication uses `HttpOnly`, `Secure`, `SameSite=Lax`, sliding
  expiration, and principal validation against active database users.
- API auth failures return 401/403 instead of MVC redirects.
- All new mutation APIs validate antiforgery tokens.
- Auth API uses a fixed-window rate limiter.
- Admin APIs require the `Admin` role.
- Order queries scope customer access by the current user ID.
- Order creation reloads prices, product state, and stock from the database.
- API contracts use explicit DTOs; generated database entities are not exposed.
- Model binding validation returns `ValidationProblemDetails`.
- Business failures map consistently to 400, 404, 409, 401, or 403.
- Unexpected API exceptions return a generic 500 response with `traceId`.
- API responses include `X-Trace-Id`; log scopes include the same identifier.
- Product descriptions are Razor-encoded instead of rendered with `Html.Raw`.
- No credentialed permissive CORS policy is configured.
- Tracked configuration contains no real API key or SQL password.

## Review By Risk

| Risk | Status | Notes |
|---|---|---|
| SQL injection | Controlled | EF LINQ uses parameterized queries; no raw SQL found in Web source |
| Mass assignment | Controlled for API | Request DTOs expose only allowed fields |
| Customer order IDOR | Controlled | `IOrderService` filters by current user ID |
| Admin function access | Controlled | `/api/v1/admin/*` requires `Admin` role |
| CSRF | Controlled for new API | Token endpoint plus validation on all mutations |
| Client price tampering | Controlled | Cart/order price is calculated server-side |
| Error data leakage | Controlled for API | Central generic exception handler |
| XSS product description | Fixed | Razor encoding replaces raw HTML rendering |
| Secret leakage | Improved | Development connection string moved out of tracked config |
| Brute-force login | Mitigated | Fixed-window rate limit and generic login error |

## Remaining Risks

1. Legacy Admin image uploads trust extension/content type too much and need
   signature validation, strict size limits, randomized extensions, and malware
   scanning before production.
2. Some legacy MVC AJAX mutations do not consistently use antiforgery
   validation. The new API endpoints are protected; remaining MVC actions
   should be audited before internet exposure.
3. Checkout has no persistent idempotency key. A rapid double submit can create
   duplicate orders in separate requests. The UI should disable submit now; a
   database-backed unique checkout key is the recommended fix.
4. Integration tests use EF InMemory, which does not verify SQL Server
   transactions, unique filtered indexes, row-version concurrency, or query
   translation. Add SQL Server Testcontainers coverage.
5. Production CSP currently permits inline scripts and `unsafe-eval` for
   compatibility with legacy views. Migrate scripts to static files and use a
   nonce-based CSP.
6. Rate limiting is process-local. A distributed limiter or gateway policy is
   needed when scaling to multiple instances.
7. Admin audit rows currently do not record client IP in application services.
   Add a privacy-reviewed request metadata abstraction if that field is needed.

## Database-First Impact

No generated entity was manually changed as part of the API-first phase. No EF
migration is introduced. The application continues to rely on the externally
managed SQL schema in `DB_Fixed.sql`.
