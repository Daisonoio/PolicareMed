using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PoliCare.Core.Interfaces;
using PoliCare.Infrastructure.Data;
using PoliCare.Infrastructure.Repositories;
using PoliCare.Services.Interfaces;
using PoliCare.Services.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PoliCareDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Repository pattern registration
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business services registration
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// NEW: Authentication services registration
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured");
}

var key = Encoding.ASCII.GetBytes(secretKey);

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero, // Remove delay of token when expired
        RequireExpirationTime = true
    };

    // Handle JWT events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Authentication failed: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst("userId")?.Value;
            logger.LogDebug("JWT Token validated for user: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Challenge triggered: {Error}", context.Error);
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    // Define role-based policies
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireClaim("role", "SuperAdmin"));

    options.AddPolicy("AdminOrAbove", policy =>
        policy.RequireClaim("role", "SuperAdmin", "PlatformAdmin", "ClinicOwner"));

    options.AddPolicy("ClinicStaff", policy =>
        policy.RequireClaim("role", "SuperAdmin", "PlatformAdmin", "ClinicOwner", "ClinicManager", "AdminStaff"));

    options.AddPolicy("MedicalStaff", policy =>
        policy.RequireClaim("role", "SuperAdmin", "PlatformAdmin", "ClinicOwner", "ClinicManager", "Doctor", "Nurse"));

    options.AddPolicy("AllStaff", policy =>
        policy.RequireClaim("role", "SuperAdmin", "PlatformAdmin", "ClinicOwner", "ClinicManager", "AdminStaff", "Doctor", "Nurse", "Receptionist"));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Enhanced Swagger configuration with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PoliCare API",
        Version = "v1",
        Description = "API per gestione poliambulatori con Smart Scheduling Engine"
    });

    // JWT Authentication support in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PoliCareDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PoliCare API v1");
        options.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

// CORS
app.UseCors();

// Authentication & Authorization
app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// Background service for cleanup expired sessions
app.Services.CreateScope().ServiceProvider.GetRequiredService<IJwtService>()
    .CleanupExpiredSessionsAsync(); // Run once at startup

app.Run();