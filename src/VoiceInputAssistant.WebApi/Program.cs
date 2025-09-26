using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using VoiceInputAssistant.WebApi.Data;
using VoiceInputAssistant.WebApi.Services;
using AspNetCoreRateLimit;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Add services to the container

// Add Database Context with migrations
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("VoiceInputAssistant.WebApi")));
builder.Services.AddDbContext<VoiceInputAssistant.Infrastructure.Data.ApplicationDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("VoiceInputAssistant.WebApi")));

builder.Services.AddControllers(options =>
{
    // Require authentication by default
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Voice Input Assistant API",
        Version = "v1",
        Description = "REST and gRPC APIs for Voice Input Assistant integration",
        Contact = new OpenApiContact
        {
            Name = "Voice Input Assistant",
            Email = "support@voiceinputassistant.com"
        }
    });

    // Include XML comments for API documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
if (builder.Environment.IsProduction() && (jwtKey.Length < 32 || jwtKey.Contains("DevKey") || jwtKey == "VoiceInputAssistant_SuperSecretKey_2024_MinimumLength32Characters!"))
{
    throw new InvalidOperationException("A secure JWT Key with at least 32 characters must be configured in production.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VoiceInputAssistant";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VoiceInputAssistant";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Add CORS for web dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebDashboard", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("VoiceAssistant:Security:AllowedOrigins").Get<string[]>() ?? new string[0];
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Register application services
RegisterApplicationServices(builder.Services, builder.Configuration);

// Add health checks  
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseIpRateLimiting();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voice Input Assistant API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("WebDashboard");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<VoiceAssistantGrpcService>();

// Add gRPC-Web support for browser clients
app.UseGrpcWeb();

// Health check endpoint
app.MapHealthChecks("/health");

// Serve static files for web dashboard (if included)
app.UseStaticFiles();

app.Run();

static void RegisterApplicationServices(IServiceCollection services, IConfiguration configuration)
{
    // Register repositories first
    services.AddScoped<VoiceInputAssistant.Infrastructure.Repositories.AdaptiveLearningRepository>();
    services.AddScoped<VoiceInputAssistant.Infrastructure.Repositories.UserFeedbackRepository>();
    
    // Register core services
    services.AddScoped<ISpeechRecognitionService, WhisperSpeechRecognitionService>();
    services.AddScoped<IApplicationProfileService, ApplicationProfileService>();
    services.AddScoped<IUsageAnalyticsService, UsageAnalyticsService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IUserManagementService, UserManagementService>();
    services.AddScoped<VoiceInputAssistant.Core.Services.Interfaces.IAdaptiveLearningService, VoiceInputAssistant.Infrastructure.Services.PersistentAdaptiveLearningService>();
    
    // Add a stub implementation for IUserFeedbackService if it doesn't exist
    // This is needed for the adaptive learning service dependency
    services.AddScoped<VoiceInputAssistant.Core.Interfaces.IUserFeedbackService, VoiceInputAssistant.Infrastructure.Services.UserFeedbackService>();
    
    // Configure settings
    services.Configure<VoiceInputAssistant.Infrastructure.Configuration.WhisperSettings>(configuration.GetSection("Whisper"));
    services.Configure<VoiceInputAssistant.Infrastructure.Configuration.EmailSettings>(configuration.GetSection("Email"));
    services.Configure<VoiceInputAssistant.Infrastructure.Configuration.TokenSettings>(configuration.GetSection("Jwt"));
    
    // Add HTTP client for external API calls
    services.AddHttpClient();

    // Add memory caching for better performance
    services.AddMemoryCache();

    // Add configuration
    services.AddOptions();
}
