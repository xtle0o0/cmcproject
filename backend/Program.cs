using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using backend.Data;
using backend.Services;
using backend.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly);

// Add CORS service
builder.Services.AddCors();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Services
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<DataImportService>();

// Configure Authentication
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not found in configuration.")))
    };
});

// Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.Logger.LogInformation("Available routes:");
    var endpointRouteBuilder = app as IEndpointRouteBuilder;
    var routes = endpointRouteBuilder?.DataSources
        .SelectMany(ds => ds.Endpoints)
        .OfType<RouteEndpoint>()
        .Select(e => e.RoutePattern.RawText);
    foreach (var route in routes ?? Enumerable.Empty<string>())
    {
        app.Logger.LogInformation(route);
    }
}

// Configure CORS for frontend
app.UseCors(policy =>
{
    policy.WithOrigins("http://localhost:3000")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Simple health check endpoint with response time
app.MapGet("/health", () =>
{
    var startTime = DateTime.UtcNow;
    
    // Create response object
    var response = new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
    };
    
    return Results.Ok(response);
})
.WithName("HealthCheck")
.WithOpenApi();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        
        // Seed data from CSV
        var dataImportService = services.GetRequiredService<DataImportService>();
        var csvFilePath = Path.Combine(AppContext.BaseDirectory, builder.Configuration["DataImport:CsvFilePath"] ?? "data.csv");
        await dataImportService.ImportUsersFromCsvAsync(csvFilePath);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();
