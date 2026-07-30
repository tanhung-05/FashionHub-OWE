# Architecture Guidelines

## Layered Architecture

### 1. Presentation Layer (MVC)
- **Controllers**: Thin controllers, delegate business logic to services
- **Views**: Razor views with partial views and view components
- **ViewModels**: DTOs for data transfer between controller and view
- **Areas**: Admin functionality isolated in Areas/Admin/

### 2. Data Access Layer
- **DbContext**: `ApplicationDbContext` - single context for entire app
- **Models**: Database-first scaffolded models in `Models/Generated/`
- **Strategy**: Database-first - scaffold from existing SQL Server database
  ```bash
  dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Models/Generated -c ApplicationDbContext --context-dir Data --force
  ```

### 3. Business Logic Layer
- **Services**: Business logic extracted to service classes
  - `IChatAiService` / `ChatAiService`: AI chatbot logic
  - `IImageFeatureService` / `ImageFeatureService`: (DISABLED) Image search
- **Service Registration**: Scoped lifetime in `Program.cs`

## Authentication & Authorization

### Cookie Authentication (NOT JWT)
```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });
```

### Password Hashing
- **Library**: BCrypt.Net-Next
- **Usage**: `BCrypt.Net.BCrypt.HashPassword()` and `BCrypt.Net.BCrypt.Verify()`
- **Do NOT change** to ASP.NET Core Identity hash - maintain compatibility

### Authorization
- Role-based: `[Authorize(Roles = "Admin")]`
- Custom checks in controller actions when needed

## API Integration

### Gemini AI Chat
- **Endpoint**: `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent`
- **Authentication**: API key via `GeminiAI:ApiKey` configuration
- **Service**: `ChatAiService` handles all Gemini interactions
- **Features**: Order tracking, product recommendations, general chat

## Configuration Management

### User Secrets (Development)
```bash
cd FashionHub2/FashionHub.Web
dotnet user-secrets set "GeminiAI:ApiKey" "your_key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your_connection"
```

### Environment Variables (Production)
```yaml
# docker-compose.yml
environment:
  - ConnectionStrings__DefaultConnection=Server=...
  - GeminiAI__ApiKey=${GEMINI_API_KEY}
  - ASPNETCORE_ENVIRONMENT=Production
```

### appsettings hierarchy
1. `appsettings.json` - base settings
2. `appsettings.Development.json` - dev overrides
3. `appsettings.Production.json` - prod overrides
4. User Secrets - local dev secrets
5. Environment Variables - deployment secrets

## Performance Optimizations

### Already Implemented
- Response compression (Gzip, production only)
- Static file caching (1 year, production only)
- Database connection pooling
- Database retry policy (3 retries, 5s delay)
- Memory caching (`IMemoryCache`)

### Recommended for Future
- Database indexes (see `docs/database-indexes-production.sql`)
- Distributed caching (Redis)
- Output caching for product catalog
- CDN for static assets
- Lazy loading for images

## Testing Strategy

### Test Infrastructure
- **Framework**: xUnit
- **Mocking**: In-memory database
- **Factory**: `CustomWebApplicationFactory<Program>` for integration tests
- **Test Environment**: `ASPNETCORE_ENVIRONMENT=Test`

### Test Coverage
- Unit tests: Individual controller actions
- Integration tests: Full shopping flows
- **DO NOT mock** when integration test is more appropriate

## Areas Pattern

### Admin Area Structure
```
Areas/Admin/
├── Controllers/     # Admin controllers with [Area("Admin")]
└── Views/
    ├── Categories/
    ├── Coupons/
    ├── Dashboard/
    ├── Orders/
    ├── Products/
    ├── Reports/
    └── Users/
```

### Routing
```csharp
// Admin routes MUST come before default routes
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

## Dependency Injection

### Service Lifetimes
- **Scoped**: `ApplicationDbContext`, `IChatAiService`
- **Singleton**: `IMemoryCache`, `IHttpClientFactory`
- **Transient**: Not commonly used in this project

### Registration Pattern
```csharp
// Program.cs
builder.Services.AddScoped<IServiceInterface, ServiceImplementation>();
```

## Health Checks

### Implemented Checks
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("GeminiAI", () => /* check API key configured */);

app.MapHealthChecks("/health");
```

### Usage
- Endpoint: `GET /health`
- Docker: Used in health check probes
- Monitoring: Can integrate with external monitoring tools

## Error Handling

### Global Exception Handler
- **Development**: `UseDeveloperExceptionPage()` - detailed errors
- **Production**: `UseExceptionHandler("/Home/Error")` - user-friendly errors
- **Status Codes**: `UseStatusCodePagesWithReExecute("/Home/Error/{0}")`

### Logging
- **Provider**: Built-in ASP.NET Core logging
- **Levels**: Debug (dev), Warning (prod)
- **DO NOT log**: PII, passwords, API keys

## Session Management

### Configuration
```csharp
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
```

### Usage
- Shopping cart state
- User preferences
- Temporary data between requests

## Security Headers

### Implemented Headers
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: SAMEORIGIN`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Content-Security-Policy` (production only)
- `HSTS` (production only)

## ViewComponents

### Existing Components
1. **CartIconViewComponent**: Display cart item count in header
2. **MenuViewComponent**: Dynamic navigation menu

### Usage
```cshtml
@await Component.InvokeAsync("CartIcon")
@await Component.InvokeAsync("Menu")
```

### When to Create New ViewComponent
- Reusable UI with server-side logic
- Too complex for partial view
- Used in multiple places
- Needs dependency injection
