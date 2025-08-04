using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PoliCare.Core.Interfaces;
using PoliCare.Infrastructure.Data;
using PoliCare.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PoliCare API",
        Version = "v1",
        Description = "API per gestione poliambulatori"
    });
});

// Database Configuration
builder.Services.AddDbContext<PoliCareDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly("PoliCare.Infrastructure") // Assicurati che sia esatto
    )
);

// Repository Pattern con Logging
builder.Services.AddScoped<IUnitOfWork>(provider =>
{
    var context = provider.GetRequiredService<PoliCareDbContext>();
    var logger = provider.GetRequiredService<ILogger<UnitOfWork>>();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    return new UnitOfWork(context, logger, loggerFactory);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PoliCare API v1");
        c.RoutePrefix = string.Empty; // Swagger disponibile su root "/"
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();