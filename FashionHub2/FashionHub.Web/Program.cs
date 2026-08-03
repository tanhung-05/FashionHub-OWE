using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Accounts;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Chat;
using FashionHub.Web.Application.Email;
using FashionHub.Web.Application.Products;
using FashionHub.Web.Application.Orders;
using FashionHub.Web.Application.Payments;
using FashionHub.Web.Data;
using FashionHub.Web.Infrastructure.Authentication;
using FashionHub.Web.Infrastructure.Cart;
using FashionHub.Web.Infrastructure.Email;
using FashionHub.Web.Infrastructure.Web;
using FashionHub.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://httpstatuses.com/400",
                Title = "Request validation failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem);
        };
    });
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GeminiChat", client =>
{
    // ChatAiService owns a linked timeout so request cancellation remains observable.
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "FashionHub.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
    options.AddPolicy("chat", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"session:{httpContext.Session.Id}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Too many requests",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = "Vui lòng thử lại sau.",
                Instance = context.HttpContext.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.HttpContext.TraceIdentifier
                }
            },
            cancellationToken);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FashionHub API",
        Version = "v1",
        Description = "Versioned REST API for the FashionHub e-commerce application."
    });
    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "FashionHub.Auth",
        Description = "Cookie authentication created by /api/v1/auth/login."
    });
});

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Database configuration with retry policy
// Skip SQL Server registration in Test environment (tests will configure InMemory)
if (builder.Environment.EnvironmentName != "Test")
{
    var defaultConnection =
        builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(defaultConnection))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured. " +
            "For local development, run: dotnet user-secrets set " +
            "\"ConnectionStrings:DefaultConnection\" \"<your SQL Server connection string>\".");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            defaultConnection,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        }));
}

// Response compression for production
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/css", "application/javascript", "image/svg+xml" });
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Memory cache for performance
builder.Services.AddMemoryCache();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("GeminiAI", () =>
    {
        var apiKey = builder.Configuration["GeminiAI:ApiKey"];
        return string.IsNullOrEmpty(apiKey)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Gemini API key not configured")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    })
    .AddCheck("VnPay", () =>
    {
        var tmnCode = builder.Configuration["VnPay:TmnCode"];
        var hashSecret = builder.Configuration["VnPay:HashSecret"];
        var returnUrl = builder.Configuration["VnPay:ReturnUrl"];
        return string.IsNullOrWhiteSpace(tmnCode)
            || string.IsNullOrWhiteSpace(hashSecret)
            || string.IsNullOrWhiteSpace(returnUrl)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("VNPAY is not configured")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    });

// Application services
builder.Services.AddScoped<IChatAiService, ChatAiService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatContextProvider, ChatContextProvider>();
builder.Services.AddScoped<IChatConversationStore, ChatConversationStore>();
builder.Services.AddSingleton<IChatFaqProvider, ChatFaqCatalog>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartSessionStore, HttpCartSessionStore>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IPasswordResetLinkFactory, PasswordResetLinkFactory>();
builder.Services.AddScoped<IAuthenticationSessionService, CookieAuthenticationSessionService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient(VnPayService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.Configure<PasswordResetOptions>(
    builder.Configuration.GetSection(PasswordResetOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<GeminiAiOptions>(
    builder.Configuration.GetSection(GeminiAiOptions.SectionName));
builder.Services.Configure<VnPayOptions>(
    builder.Configuration.GetSection(VnPayOptions.SectionName));
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderCancellationService, OrderCancellationService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IPendingPaymentReconciliationService,
    PendingPaymentReconciliationService>();
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHostedService<PendingPaymentReconciliationWorker>();
}
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<IAdminProductService>(
    provider => provider.GetRequiredService<AdminService>());
builder.Services.AddScoped<IAdminOrderService>(
    provider => provider.GetRequiredService<AdminService>());
builder.Services.AddScoped<IAdminReportService>(
    provider => provider.GetRequiredService<AdminService>());
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "FashionHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var emailClaim = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var roleClaim = context.Principal?.FindFirstValue(ClaimTypes.Role);
            var securityStampClaim = context.Principal?.FindFirstValue("SecurityStamp");
            var isValidPrincipal = false;

            if (int.TryParse(userIdClaim, out var userId)
                && Guid.TryParse(securityStampClaim, out var securityStamp)
                && !string.IsNullOrWhiteSpace(emailClaim)
                && !string.IsNullOrWhiteSpace(roleClaim))
            {
                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

                var currentSession = await dbContext.NguoiDungs
                    .AsNoTracking()
                    .Where(user =>
                        user.IdnguoiDung == userId
                        && user.Email == emailClaim
                        && user.SecurityStamp == securityStamp
                        && user.TrangThai
                        && user.DeletedAt == null)
                    .Select(user => new
                    {
                        Role = user.IdvaiTroNavigation.TenVaiTro
                    })
                    .SingleOrDefaultAsync();

                isValidPrincipal = currentSession is not null
                    && string.Equals(
                        currentSession.Role,
                        roleClaim,
                        StringComparison.Ordinal);
            }

            if (!isValidPrincipal)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FashionHub API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
    app.UseHsts();
    
    // Response compression for production only
    app.UseResponseCompression();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://code.jquery.com https://cdn.jsdelivr.net https://generativelanguage.googleapis.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data: https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
            "connect-src 'self' https://cdn.jsdelivr.net https://fonts.googleapis.com https://fonts.gstatic.com https://generativelanguage.googleapis.com;");
    }
    
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    apiPipeline => apiPipeline.UseExceptionHandler());

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    apiPipeline => apiPipeline.Use(async (context, next) =>
    {
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FashionHub.Api");
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = context.TraceIdentifier,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path.Value ?? string.Empty
        }))
        {
            await next();
        }
    }));

// Static files with caching for production
if (app.Environment.IsProduction())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Cache static files for 1 year
            const int durationInSeconds = 60 * 60 * 24 * 365;
            ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
        }
    });
}
else
{
    app.MapStaticAssets();
}

app.UseSession();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration
            })
        });
    }
});

app.Run();

// Make the implicit Program class public for integration testing
public partial class Program { }
