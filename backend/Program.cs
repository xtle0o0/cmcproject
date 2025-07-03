var builder = WebApplication.CreateBuilder(args);

// Add CORS service
builder.Services.AddCors();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure CORS for frontend
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

// Enable Swagger/OpenAPI for API documentation
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

app.Run();
