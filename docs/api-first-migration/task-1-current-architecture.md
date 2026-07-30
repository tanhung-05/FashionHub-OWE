# Task 1 - Current Architecture Assessment

## 1. Summary

FashionHub is a single-solution ASP.NET Core MVC application that has already
been migrated to .NET 10. It uses Entity Framework Core 10 with SQL Server and
database-first generated entities. Customer MVC, Admin Area MVC, Razor views,
application services, EF mappings, and tests currently live in two projects:

```text
FashionHub2/
|-- FashionHub.Web/       ASP.NET Core MVC application
`-- FashionHub.Tests/     xUnit and WebApplicationFactory tests
```

The migration should remain inside the existing web project. Splitting the
application into multiple class libraries now would add movement without
removing the current coupling. The target is a modular monolith in which MVC
controllers and versioned API controllers share application services.

## 2. Technical Baseline

| Concern | Current implementation |
|---|---|
| Runtime | .NET 10 (`net10.0`) |
| Web framework | ASP.NET Core MVC |
| ORM | Entity Framework Core 10.0.9 |
| Database | SQL Server, `QL_SHOPQUANAO_PRO` |
| Data strategy | Database-first entities in `Models/Generated` |
| DI | Built-in ASP.NET Core container, scoped application services |
| Authentication | Cookie authentication (`FashionHub.Auth`) |
| Authorization | `[Authorize]` and role-based `[Authorize(Roles = "Admin")]` |
| Password hashing | BCrypt.Net-Next 4.2.0 |
| Session | ASP.NET Core session, seven-day idle timeout |
| Cart | Guest cart in session; authenticated cart in `GioHang`; merge on login |
| Routing | Conventional Admin Area route followed by default MVC route |
| Error handling | Developer exception page in Development; MVC error page in Production |
| Health | EF Core and Gemini health checks at `/health` |
| API documentation | Not present |
| Tests | xUnit, WebApplicationFactory, EF Core InMemory |

Evidence:

```text
Command:
Get-Content FashionHub2/FashionHub.Web/FashionHub.Web.csproj

Output:
<TargetFramework>net10.0</TargetFramework>
Microsoft.EntityFrameworkCore.SqlServer 10.0.9
BCrypt.Net-Next 4.2.0
```

```text
Command:
rg -i "swagger|swashbuckle|openapi" FashionHub2/FashionHub.Web

Output:
SWAGGER_OPENAPI_MATCHES=0
```

The Development connection string is currently stored in
`appsettings.Development.json`. It contains Windows integrated-security machine
configuration rather than a password, but should still move to User Secrets so
another developer can run the repository without editing a tracked file.

## 3. Current Request Flow

```text
Browser
  |
  +-- MVC controller -----------+
  |                              |
  +-- Razor form/AJAX            v
                         ApplicationDbContext
                                  |
                                  v
                              SQL Server

Cart exception:
MVC CartController --> ICartService --> Session or ApplicationDbContext

Chat exception:
MVC ChatController --> IChatAiService --> DbContext + Gemini HTTP API
```

Most business modules still use `ApplicationDbContext` directly from their
controllers. The cart is the first module with a meaningful shared service.

Evidence:

```text
Command:
Count controller files and files containing ApplicationDbContext

Output:
CONTROLLERS=13
CONTROLLERS_WITH_DB_CONTEXT=12
```

## 4. Module Dependency Map

| Module | Controllers | Existing service/repository | Main data | Dependencies | Migration risk |
|---|---|---|---|---|---|
| Home/catalog | `HomeController`, `ProductsController` | None | Product, variant, image, category, brand, color, size | Cart modal and product views | Medium |
| Cart | `CartController` | `ICartService`, `CartService`; no repository | Cart, product, variant, image, user | Session, claims, login merge, checkout | Medium |
| Checkout/orders | `OrderController` | Reuses cart only | Order, order item, stock, coupon, address, payment, histories | Authentication, cart, inventory | High |
| Authentication/profile | `AccountController` | Reuses cart merge only | User, role, address, order | Cookie auth, BCrypt, cart, orders | High |
| Chat | `ChatController` | `IChatAiService` | Product/order context | Gemini API, current user | Low |
| Admin products | `Areas/Admin/ProductsController` | None | Product, variant, image, stock history | File system, catalog, inventory | High |
| Admin categories | `Areas/Admin/CategoriesController` | None | Category, product | Slugs, soft delete | Medium |
| Admin coupons | `Areas/Admin/CouponsController` | None | Coupon, order | Checkout discount rules | Medium |
| Admin orders | `Areas/Admin/OrdersController` | None | Order, item, status, stock histories | Inventory and audit | High |
| Dashboard/reports | Dashboard and Reports controllers | None | Orders, users, products, order items | Shared status constants | Medium |
| Admin users | `Areas/Admin/UsersController` | None | User, role, order | Cookie/role status | Medium |

There is no repository layer. Adding a generic repository is not recommended:
EF Core already supplies unit-of-work and query composition. Application
services should use `ApplicationDbContext` directly and project into DTOs.

## 5. Architectural Problems

### 5.1 Controller and data-access coupling

Twelve controllers inject the DbContext directly. Catalog projections,
checkout rules, order state transitions, stock mutation, coupon calculations,
authentication, and admin workflows are therefore tied to MVC action methods.
They cannot be reused safely by API controllers.

Largest controller files:

```text
752 Areas/Admin/Controllers/ProductsController.cs
664 Controllers/AccountController.cs
439 Controllers/OrderController.cs
410 Areas/Admin/Controllers/OrdersController.cs
268 Controllers/CartController.cs
```

### 5.2 Business logic in presentation

- Product filtering, image selection, availability, and price presentation are
  assembled in `ProductsController` and repeated in `HomeController`.
- `OrderController` validates stock, recalculates coupon discounts, creates an
  order, writes order and inventory histories, mutates stock, and clears cart.
- `AccountController` owns authentication, profile/address rules, order mapping,
  and customer cancellation with inventory restoration.
- Admin product and order controllers perform multi-table business operations.

### 5.3 Inconsistent cart presentation

`CartService` uses the `CartSession` key and database cart behavior, while
`CartIconViewComponent` still reads a legacy `Cart` session dictionary. The
component should call the shared cart service.

### 5.4 Error semantics are MVC-only

Production exceptions are redirected to `/Home/Error`. There is no API
`ProblemDetails` handler, trace extension, or business-exception mapping.
Several controllers catch broad exceptions and convert them to generic UI
messages; API controllers must not copy this pattern.

### 5.5 Missing API safeguards

- No Swagger/OpenAPI.
- No versioned attribute routes.
- No API DTO boundary.
- No centralized API validation response.
- Cookie-authenticated mutation endpoints need a documented antiforgery flow.
- No CORS configuration is currently present, which is appropriate while MVC
  and API remain same-origin.

### 5.6 Test fidelity

The current WebApplicationFactory suite is useful for regression, but EF Core
InMemory does not enforce SQL Server foreign keys, transactions, filtered
indexes, rowversion behavior, or relational translation. Keep it for fast API
tests and add a small SQL Server integration suite later when Docker or a test
SQL Server is available.

Evidence:

```text
Command:
Count generated models, services, and declared test methods

Output:
GENERATED_MODEL_FILES=22
SERVICE_FILES=6
DECLARED_TEST_METHODS=35
```

## 6. Reusable Components

- `ApplicationDbContext` and the current database-first mappings.
- `ICartService` / `CartService`, after introducing current-user/session
  abstractions and API DTO mapping.
- Cookie authentication, role claims, and BCrypt hashes.
- Existing customer/admin ViewModels where they are presentation-specific.
- `CommerceConstants`, including order statuses, coupon types, and shipping fee.
- `SlugGenerator`.
- `CustomWebApplicationFactory<Program>` and controlled test seed data.
- Existing MVC URLs, views, partials, forms, and antiforgery behavior.

Generated entities remain internal to data/application code and must not be
returned by API actions.

## 7. High-Risk Areas

1. Checkout and order creation: transaction, price authority, coupon validity,
   inventory concurrency, and cart clearing must remain atomic.
2. Order status transitions: cancelling or reopening orders changes inventory
   and histories and must be shared between MVC and Admin API.
3. Cookie API mutations: CSRF protection must work for MVC today and a future
   browser SPA.
4. Object-level authorization: customer order/address queries must always be
   scoped to the authenticated user.
5. Image upload: extension, content type, size, generated filename, and storage
   path require a dedicated service before exposing an Admin API.
6. Database-first regeneration: application behavior must not be added to
   generated entity files.
7. InMemory tests can pass behavior that SQL Server rejects.

## 8. Recommended Target Architecture

Keep one deployable web project and introduce clear folders:

```text
FashionHub.Web/
|-- Application/
|   |-- Common/
|   |-- Products/
|   |-- Cart/
|   |-- Orders/
|   |-- Authentication/
|   `-- Admin/
|-- Controllers/                 Existing MVC controllers
|-- Controllers/Api/V1/          Public API controllers
|-- Areas/Admin/Controllers/     Existing Admin MVC controllers
|-- Areas/Admin/Controllers/Api/V1/
|-- Infrastructure/
|   |-- Authentication/
|   `-- Web/
|-- Data/
|-- Models/Generated/
`-- Views/
```

Target request flow:

```text
MVC Controller ----+
                   +--> Application Service --> ApplicationDbContext --> SQL Server
API Controller ----+
```

Use simple service results for expected cart-style business outcomes and
business exceptions for resource/not-found/conflict cases handled centrally as
`ProblemDetails`. Controllers remain responsible only for HTTP or View
translation. Do not add AutoMapper or a generic repository.

## 9. Refactoring Sequence

1. Add API infrastructure: controller registration, ProblemDetails, Swagger,
   API antiforgery endpoint/filter, DTO conventions, pagination, current user.
2. Extract catalog query logic to `IProductService`; make MVC Products and Home
   consume it; add public Products API.
3. evolve `ICartService` so it returns application models independent of MVC;
   make MVC, API, and cart icon consume the same service.
4. Extract order query/checkout orchestration into `IOrderService`; preserve the
   MVC form and expose authenticated Orders API.
5. Extract auth/profile behavior and expose cookie-based Auth API.
6. Extract Admin product/order/report services and add role-protected APIs.
7. Expand integration tests, add security review, Swagger docs, README, and
   migration status.

## 10. Expected File Impact

Files expected to be created:

- `Application/Common/*`
- `Application/Products/*`
- `Application/Cart/*`
- `Application/Orders/*`
- `Application/Authentication/*`
- `Controllers/Api/V1/*`
- `Areas/Admin/Controllers/Api/V1/*`
- `Infrastructure/Web/*`
- API integration test files
- API architecture and endpoint documentation

Files expected to be modified:

- `Program.cs`
- MVC Products, Home, Cart, Order, and Account controllers
- Admin Products, Orders, Dashboard, Reports, Categories, Coupons, and Users
  controllers as their services are introduced
- `CartIconViewComponent`
- `FashionHub.Web.csproj`
- `CustomWebApplicationFactory.cs`
- `README.md`

Database impact for the API migration itself: none. Database-first mappings and
`DB_Fixed.sql` remain the source of truth.

## 11. Compatibility Impact

- MVC: kept active throughout migration; routes and views remain.
- Database: no migration or schema change in Task 1.
- Authentication: cookie authentication remains; JWT is explicitly excluded.
- Deployment: still one ASP.NET Core application.

## 12. Baseline Verification

```text
Command:
cd FashionHub2; dotnet build --no-incremental

Output:
Build succeeded.
21 Warning(s)
0 Error(s)
```

```text
Command:
cd FashionHub2; dotnet test --no-build --logger "console;verbosity=minimal"

Output:
Passed! - Failed: 0, Passed: 35, Skipped: 0, Total: 35
```

## 13. Remaining Issues

- SQL Server execution is not available in the current environment, so
  relational integration behavior is not yet verified.
- Existing nullable and disabled Windows-only image-service warnings remain.
- The tracked Development connection string should move to User Secrets.
- Product image upload requires a dedicated security review.

## 14. Next Task

Task 2 through Task 4: introduce the shared application boundary and API
infrastructure, then complete Products and Cart as the first API-first modules.
