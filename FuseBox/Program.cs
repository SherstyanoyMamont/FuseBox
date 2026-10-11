
using System.Security.Claims;
using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using FuseBox.App.Accounts;
using FuseBox.App.Billing;
using FuseBox.App.Catalog;
using FuseBox.App.Controllers;
using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Services.Auth;
using FuseBox.App.Services.Projects;
using FuseBox.App.Services.Pricing;
using FuseBox.App.SchemaEditor;
using FuseBox.App.Subscriptions;
using FuseBox.App.Usage;
using FuseBox.Controllers;
using FuseBox.Ai;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FuseBox
{
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddFuseBoxConfiguratorAi(
                builder.Configuration);

            builder.Services.AddFuseBoxPricing(
                builder.Configuration);

            var aiDailyRequestLimit = Math.Clamp(
                builder.Configuration.GetValue<int?>(
                    "OpenAI:Configurator:DailyRequestLimit") ?? 200,
                1,
                100000);

            var aiTranscriptionDailyRequestLimit = Math.Clamp(
                builder.Configuration.GetValue<int?>(
                    "OpenAI:Configurator:TranscriptionDailyRequestLimit") ?? 200,
                1,
                100000);

            var frontendOrigins = builder.Configuration
                .GetSection("Frontend:AllowedOrigins")
                .Get<string[]>()?
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (frontendOrigins == null || frontendOrigins.Length == 0)
            {
                frontendOrigins = new[] { "http://localhost:3000" };
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(frontendOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Trust only explicitly configured reverse proxies; never all networks.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;
                foreach (var proxy in builder.Configuration
                    .GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });

            var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"];
            if (!string.IsNullOrWhiteSpace(keyDirectory))
            {
                builder.Services.AddDataProtection()
                    .SetApplicationName("FuseBox")
                    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
            }

            builder.Services.AddControllersWithViews();

            // Controllers return one stable validation envelope themselves.
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.AddAutoMapper(
                _ => { },
                typeof(FuseBoxUnitProfile).Assembly);

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMySql(
                    builder.Configuration.GetConnectionString(
                        "DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 41)));

                if (builder.Environment.IsDevelopment())
                {
                    options
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors()
                        .LogTo(
                            Console.WriteLine,
                            LogLevel.Information);
                }
            });

            builder.Services.AddScoped<
                IPasswordHasher<User>,
                PasswordHasher<User>>();

            builder.Services.AddScoped<AuthCookieEvents>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<SubscriptionService>();
            builder.Services.AddScoped<EntitlementService>();
            builder.Services.AddScoped<UsageService>();
            builder.Services.AddScoped<BillingService>();
            builder.Services.AddScoped<PaddleWebhookVerifier>();
            builder.Services.AddScoped<PaddleWebhookProcessor>();
            builder.Services.AddScoped<BillingReconciliationService>();
            builder.Services.AddScoped<BillingProductionReadinessService>();
            builder.Services.AddScoped<ManagedComponentCatalogService>();
            builder.Services.AddHostedService<ComponentCatalogBootstrapHostedService>();
            builder.Services.AddHostedService<BillingReconciliationHostedService>();
            builder.Services.AddHttpClient<PaddleClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });
            builder.Services.Configure<PlanEntitlementsOptions>(
                builder.Configuration.GetSection("Plans"));
            builder.Services.Configure<PaddleBillingOptions>(
                builder.Configuration.GetSection(
                    PaddleBillingOptions.SectionName));

            // Project persistence and ownership checks are centralized here.
            // Controllers never accept an owner from JSON.
            builder.Services.AddScoped<ProjectService>();

            // Schema editor Stage 2: separate persistence, validation and
            // optimistic-concurrency services over the existing generator.
            builder.Services.AddSingleton<
                ISchemaEditorValidationRule,
                ComponentPlacementValidationRule>();
            builder.Services.AddSingleton<
                ISchemaEditorValidationRule,
                ConnectionValidationRule>();
            builder.Services.AddSingleton<
                ISchemaEditorValidationRule,
                BasicElectricalValidationRule>();
            builder.Services.AddSingleton<
                ISchemaEditorValidationRule,
                ConsumerLinkValidationRule>();
            builder.Services.AddSingleton<SchemaEditorValidator>();
            builder.Services.AddScoped<SchemaEditorService>();

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddScoped<
                    IPasswordResetSender,
                    DevelopmentPasswordResetSender>();
            }
            else
            {
                builder.Services.AddScoped<
                    IPasswordResetSender,
                    SmtpPasswordResetSender>();
            }

            builder.Services
                .AddAuthentication(
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "FuseBox.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.EventsType = typeof(AuthCookieEvents);
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "Admin",
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireRole(UserRole.Admin.ToString());
                    });
            });

            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "FuseBox.Csrf";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.AddPolicy(
                    "password-reset",
                    httpContext =>
                    {
                        var partitionKey =
                            httpContext.Connection.RemoteIpAddress?
                                .ToString() ?? "unknown";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(15),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            });
                    });

                // Technical protection only. Commercial Free/Pro AI quota is
                // enforced separately by UsageService inside the AI endpoints.
                // Edit OpenAI:Configurator:DailyRequestLimit in appsettings.json.
                options.AddPolicy(
                    "ai-daily",
                    httpContext =>
                    {
                        var userId = httpContext.User.FindFirstValue(
                            ClaimTypes.NameIdentifier);

                        var partitionKey = string.IsNullOrWhiteSpace(userId)
                            ? $"anonymous:{httpContext.Connection.RemoteIpAddress}"
                            : $"user:{userId}";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = aiDailyRequestLimit,
                                Window = TimeSpan.FromDays(1),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            });
                    });

                // Voice transcription has a separate quota so recording a
                // message does not consume the configurator chat quota.
                options.AddPolicy(
                    "ai-transcription-daily",
                    httpContext =>
                    {
                        var userId = httpContext.User.FindFirstValue(
                            ClaimTypes.NameIdentifier);

                        var partitionKey = string.IsNullOrWhiteSpace(userId)
                            ? $"anonymous:{httpContext.Connection.RemoteIpAddress}"
                            : $"user:{userId}";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = aiTranscriptionDailyRequestLimit,
                                Window = TimeSpan.FromDays(1),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            });
                    });
            });

            builder.Services.AddEndpointsApiExplorer();

            // builder.Services.AddSwaggerGen();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("CSRF", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-CSRF-TOKEN",
                    In = ParameterLocation.Header,
                    Description = "Paste the token returned by GET /api/auth/csrf"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "CSRF"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            var accountCommandExitCode =
                await TryRunAccountCommandAsync(app, args);

            if (accountCommandExitCode.HasValue)
            {
                Environment.ExitCode = accountCommandExitCode.Value;
                return;
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            // CORS must wrap static catalog assets too: Three.js loads GLB
            // files from the backend origin during local development.
            app.UseCors("AllowFrontend");
            var staticFileContentTypes = new FileExtensionContentTypeProvider();
            staticFileContentTypes.Mappings[".glb"] = "model/gltf-binary";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = staticFileContentTypes
            });
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapFuseBoxConfiguratorAi();
            app.MapFuseBoxSchemaAi();

            await app.RunAsync();
        }

        private static async Task<int?> TryRunAccountCommandAsync(
            WebApplication app,
            string[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }

            // Explicit deployment command; normal startup never changes the schema.
            if (args.Length == 1 && string.Equals(args[0], "--migrate-database",
                StringComparison.OrdinalIgnoreCase))
            {
                await using var migrationScope = app.Services.CreateAsyncScope();
                var database = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
                await database.Database.MigrateAsync();
                Console.WriteLine("Database migrations completed.");
                return 0;
            }

            if (string.Equals(
                args[0],
                "--promote-admin",
                StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
                {
                    Console.Error.WriteLine(
                        "Usage: dotnet run -- --promote-admin user@example.com");
                    return 2;
                }

                await using var scope = app.Services.CreateAsyncScope();
                var accounts = scope.ServiceProvider
                    .GetRequiredService<AccountService>();

                var user = await accounts.PromoteToAdminAsync(args[1]);

                if (user == null)
                {
                    Console.Error.WriteLine(
                        $"No registered account found for '{args[1]}'.");
                    return 3;
                }

                Console.WriteLine(
                    $"User '{user.Email}' is now Admin. " +
                    "Existing authentication cookies for that account are invalidated.");

                return 0;
            }

            if (string.Equals(
                args[0],
                "--set-plan",
                StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3 ||
                    string.IsNullOrWhiteSpace(args[1]) ||
                    !Enum.TryParse<PlanCode>(
                        args[2],
                        ignoreCase: true,
                        out var plan))
                {
                    Console.Error.WriteLine(
                        "Usage: dotnet run -- --set-plan user@example.com free|pro");
                    return 2;
                }

                await using var scope = app.Services.CreateAsyncScope();
                var subscriptions = scope.ServiceProvider
                    .GetRequiredService<SubscriptionService>();

                var user = await subscriptions.SetPlanByEmailAsync(
                    args[1],
                    plan);

                if (user == null)
                {
                    Console.Error.WriteLine(
                        $"No registered account found for '{args[1]}'.");
                    return 3;
                }

                Console.WriteLine(
                    $"User '{user.Email}' is now on the {plan} plan. " +
                    "Refresh the frontend session to see updated entitlements.");

                return 0;
            }

            return null;
        }
    }
}
