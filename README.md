# FashionHub

FashionHub is a portfolio e-commerce application built with ASP.NET Core MVC,
Entity Framework Core, SQL Server, and cookie authentication. The codebase is
being migrated incrementally from a controller-heavy MVC monolith to an
API-first monolith while retaining the existing Razor UI.

## Architecture

Before the refactor, MVC controllers queried `ApplicationDbContext` directly,
performed business rules, and returned Razor views or ad hoc JSON. The current
request flow is:

```text
HTTP request
    |
MVC controller or /api/v1 controller
    |
Application service returning DTO + ServiceResult<T>
    |
EF Core database-first DbContext
    |
SQL Server
```

- MVC controllers remain the server-rendered user interface.
- API controllers expose versioned REST endpoints and contain HTTP concerns only.
- Application services own queries, validation, pricing, stock, and order rules.
- DTOs isolate public contracts from database-generated entities.
- `ProblemDetails` provides consistent API errors with a `traceId`.
- MVC and API call services directly; the monolith does not make internal HTTP calls.

Products, Cart, regular checkout, and login/register now share application
services. Buy Now and several Admin Razor workflows remain compatible legacy
paths and are documented in the migration notes.

## Technology

- .NET 10 and ASP.NET Core MVC/Web API
- Entity Framework Core 10 with SQL Server, database-first entities
- Cookie authentication and role authorization
- BCrypt password hashing
- xUnit, `WebApplicationFactory`, and EF Core InMemory for automated tests
- Swashbuckle/OpenAPI
- Gemini API for chat

## Local Setup

Requirements:

- .NET 10 SDK
- SQL Server or Docker Desktop
- PowerShell on Windows for the commands below

Create the database by running [`DB_Fixed.sql`](DB_Fixed.sql). The application
does not apply EF migrations because the schema is managed database-first.

Configure local secrets:

```powershell
cd FashionHub2/FashionHub.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=FashionHub;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "GeminiAI:ApiKey" "your-key"
```

Restore, build, test, and run:

```powershell
cd FashionHub2
dotnet restore
dotnet build
dotnet test
cd FashionHub.Web
dotnet run
```

Open:

- MVC: `http://localhost:5197`
- Swagger: `http://localhost:5197/swagger`
- Health check: `http://localhost:5197/health`

Swagger is enabled only in Development. Do not put real credentials or API keys
in tracked JSON files. Test-only users are created by
`CustomWebApplicationFactory`; they are not production seed accounts.

## API Authentication And CSRF

The API deliberately keeps the existing cookie authentication instead of
introducing JWT. Any client using mutation endpoints must first obtain an
antiforgery token:

```http
GET /api/v1/security/csrf-token
```

The response contains `token` and `headerName`. Send that value as
`X-CSRF-TOKEN` and include cookies on subsequent `POST`, `PUT`, or `DELETE`
requests. A browser frontend on the same origin does this with
`credentials: "include"`. No permissive credentialed CORS policy is configured.

```javascript
const csrf = await fetch("/api/v1/security/csrf-token", {
  credentials: "include"
}).then(response => response.json());

await fetch("/api/v1/cart/items", {
  method: "POST",
  credentials: "include",
  headers: {
    "Content-Type": "application/json",
    [csrf.headerName]: csrf.token
  },
  body: JSON.stringify({ variantId: 1, quantity: 2 })
});
```

After login or logout, fetch a fresh token because the authenticated identity
has changed. Login endpoints are rate-limited.

## API Endpoints

| Method | Endpoint | Authentication | Authorization | Description |
|---|---|---|---|---|
| GET | `/api/v1/security/csrf-token` | No | Public | Issue CSRF token |
| GET | `/api/v1/products` | No | Public | Filtered, sorted product page |
| GET | `/api/v1/products/{id}` | No | Public | Product detail |
| GET | `/api/v1/cart` | Optional | Current session/user | Read cart |
| POST | `/api/v1/cart/items` | Optional | CSRF | Add variant |
| PUT | `/api/v1/cart/items/{variantId}` | Optional | CSRF | Set quantity |
| DELETE | `/api/v1/cart/items/{variantId}` | Optional | CSRF | Remove variant |
| DELETE | `/api/v1/cart` | Optional | CSRF | Clear cart |
| POST | `/api/v1/auth/login` | No | CSRF, rate limit | Sign in with cookie |
| POST | `/api/v1/auth/register` | No | CSRF, rate limit | Create customer |
| POST | `/api/v1/auth/logout` | Yes | CSRF | Sign out |
| GET | `/api/v1/auth/me` | Yes | Current user | Current user DTO |
| GET | `/api/v1/account/profile` | Yes | Owner | Current profile |
| PUT | `/api/v1/account/profile` | Yes | Owner, CSRF | Update profile and refresh cookie |
| PUT | `/api/v1/account/password` | Yes | Owner, CSRF | Change password and revoke session |
| GET | `/api/v1/account/addresses` | Yes | Owner | Delivery addresses |
| POST | `/api/v1/account/addresses` | Yes | Owner, CSRF | Create delivery address |
| PUT | `/api/v1/account/addresses/{id}` | Yes | Owner, CSRF | Update owned address |
| DELETE | `/api/v1/account/addresses/{id}` | Yes | Owner, CSRF | Delete owned address |
| GET | `/api/v1/orders` | Yes | Owner | Current user's orders |
| GET | `/api/v1/orders/{id}` | Yes | Owner | Owned order detail |
| POST | `/api/v1/orders` | Yes | CSRF | Create order from server cart |
| GET | `/api/v1/admin/products` | Yes | Admin | Product management page |
| GET | `/api/v1/admin/products/{id}` | Yes | Admin | Admin product detail |
| POST | `/api/v1/admin/products` | Yes | Admin, CSRF | Create product |
| PUT | `/api/v1/admin/products/{id}` | Yes | Admin, CSRF | Update product |
| DELETE | `/api/v1/admin/products/{id}` | Yes | Admin, CSRF | Soft-delete product |
| GET | `/api/v1/admin/orders` | Yes | Admin | Search/filter orders |
| GET | `/api/v1/admin/orders/{id}` | Yes | Admin | Any order detail |
| PUT | `/api/v1/admin/orders/{id}/status` | Yes | Admin, CSRF | Valid status transition |
| GET | `/api/v1/admin/reports/dashboard` | Yes | Admin | Date-range aggregates |
| GET/POST/PUT/DELETE | `/api/v1/admin/categories` | Yes | Admin, CSRF for writes | Category management |
| GET/POST/PUT/DELETE | `/api/v1/admin/coupons` | Yes | Admin, CSRF for writes | Coupon management |
| GET | `/api/v1/admin/customers` | Yes | Admin | Customer search and detail |
| PUT | `/api/v1/admin/customers/{id}/status` | Yes | Admin, CSRF | Lock/unlock and revoke sessions |

Request and response schemas, validation constraints, and status codes are
available in Swagger. No endpoint returns an EF Core generated entity.

Example product request:

```http
GET /api/v1/products?pageNumber=1&pageSize=20&categoryId=1&sortBy=price&sortDirection=asc
```

Example order request:

```json
{
  "addressId": 1,
  "paymentMethodId": 1,
  "couponCode": "WELCOME10",
  "note": "Call before delivery"
}
```

The order service ignores client price, total, user ID, and status. It reloads
variants and prices, validates stock, writes the order and inventory history in
a SQL Server transaction, and clears the database cart only after successful
persistence.

## Tests

```powershell
cd FashionHub2
dotnet test --logger "console;verbosity=minimal"
```

On a Windows machine where Smart App Control blocks test assemblies created on
the project drive, use a temporary artifacts directory:

```powershell
dotnet test --artifacts-path "$env:TEMP\FashionHubArtifacts" `
  --logger "console;verbosity=minimal"
```

The suite covers legacy MVC regression plus Products, Cart, Orders,
Authentication, and Admin API flows. Integration tests use an isolated
in-memory database per `WebApplicationFactory`, never the configured
development or production database. SQL Server transaction/concurrency
behavior should additionally be covered by a container-based test suite before
production deployment.

## Migration Notes

- Database-first generated models and `ApplicationDbContext` remain intact.
- Existing MVC routes and Razor views remain available.
- Auth remains cookie-based; no JWT was introduced.
- Authenticated carts persist in `GioHang`; guest carts store only variant ID
  and quantity in session. Prices and stock are always reloaded from SQL.
- Regular MVC checkout now calls `IOrderService`.
- Buy Now retains its dedicated session-based MVC path.
- Existing Admin Razor controllers still contain some legacy workflow logic
  for variants, images, exports, and invoices. New Admin REST endpoints use
  `AdminService`.
- The image similarity service remains disabled because it uses Windows-only
  `System.Drawing`; an ImageSharp replacement is future work.

See the curated [`docs`](docs/) directory for deployment, configuration, and
database maintenance guides.

For a practical explanation of where the application, SQL Server, uploaded
images, secrets, backups, and monitoring live after deployment, start with
[`docs/deployment-for-beginners.md`](docs/deployment-for-beginners.md).

## Next Steps

1. Add SQL Server Testcontainers coverage for transactions and row-version conflicts.
2. Move remaining Admin MVC variant/image workflows into focused services.
3. Harden and centralize image upload validation.
4. Add checkout idempotency storage to prevent duplicate submissions.
5. Replace the Razor frontend incrementally with React or Vue against `/api/v1`.
6. Verify the GitHub Actions CI workflow, then add deployment smoke tests,
   structured log shipping, and metrics.
