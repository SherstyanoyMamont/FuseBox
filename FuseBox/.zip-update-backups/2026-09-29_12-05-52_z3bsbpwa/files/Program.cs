using FuseBox.Ai;
using FuseBox.App.Controllers;
using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Services.Auth;
using FuseBox.App.Services.Projects;
using FuseBox.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

namespace FuseBox
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddFuseBoxConfiguratorAi(builder.Configuration);

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

            builder.Services.AddControllersWithViews();

            // Controllers return one stable validation envelope themselves.
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.AddAutoMapper(
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

            // Stage 6: all project persistence and ownership checks are
            // centralized here. Controllers never accept an owner from JSON.
            builder.Services.AddScoped<ProjectService>();

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
                    options.Cookie.SameSite = SameSiteMode.Lax;

                    options.Cookie.SecurePolicy =
                        builder.Environment.IsDevelopment()
                            ? CookieSecurePolicy.SameAsRequest
                            : CookieSecurePolicy.Always;

                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.EventsType = typeof(AuthCookieEvents);
                });

            builder.Services.AddAuthorization();

            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "FuseBox.Csrf";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.Cookie.SecurePolicy =
                    builder.Environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
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

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapFuseBoxConfiguratorAi();
            app.Run();
        }
    }
}
