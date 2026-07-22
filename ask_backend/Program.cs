using System.Text;
using System.Threading.RateLimiting;
using ASK.Application;
using ASK.Infrastructure;
using ASK.Infrastructure.Persistence;
using ask_backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────────
// 1. KATMAN DI KAYITLARI (Onion Architecture)
// ────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ────────────────────────────────────────────────────
// 2. CONTROLLERS
// ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ────────────────────────────────────────────────────
// 3. JWT KİMLİK DOĞRULAMA (OWASP A07)
// ────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero  // Token süresine ekstra esneklik verme
    };
});

builder.Services.AddAuthorization();

// ────────────────────────────────────────────────────
// 4. CORS (Frontend erişimi)
// ────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ────────────────────────────────────────────────────
// 5. RATE LIMITING (OWASP A05, Brute-force koruması)
// ────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Auth endpoint'leri için sıkı limit
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    // Genel API limit
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 120;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 5;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ────────────────────────────────────────────────────
// 6. SWAGGER (JWT destekli)
// ────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ASK B2B API",
        Version = "v1",
        Description = "ASK E-Ticaret B2B Backend API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Token giriniz: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ────────────────────────────────────────────────────
// 7. PIPELINE MIDDLEWARE SIRASI (sıra kritik!)
// ────────────────────────────────────────────────────

// Global exception handler (ilk olmalı)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Güvenli HTTP başlıkları (OWASP A05)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASK B2B API v1"));
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ────────────────────────────────────────────────────
// 8. VERITABANI MİGRASYON (Development ortamında otomatik)
// ────────────────────────────────────────────────────
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
try
{
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS `UserCategoryDiscounts` (
            `Id` INT NOT NULL AUTO_INCREMENT,
            `UserId` INT NOT NULL,
            `CategoryId` INT NOT NULL,
            `DiscountRate` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
            `CreatedAt` DATETIME(6) NOT NULL,
            `UpdatedAt` DATETIME(6) NOT NULL,
            PRIMARY KEY (`Id`),
            KEY `IX_UserCategoryDiscounts_UserId` (`UserId`),
            KEY `IX_UserCategoryDiscounts_CategoryId` (`CategoryId`),
            CONSTRAINT `FK_UserCategoryDiscounts_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
            CONSTRAINT `FK_UserCategoryDiscounts_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
    ");
}
catch (Exception ex)
{
    Console.WriteLine($"UserCategoryDiscounts table check: {ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    try { await db.Database.MigrateAsync(); } catch {}
}

// Database migrations completed.

app.Run();
