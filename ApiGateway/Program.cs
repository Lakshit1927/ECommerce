using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔐 JWT Authentication Configuration (for gateway-level auth)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];

if (!string.IsNullOrEmpty(secretKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
}

// 🌐 YARP Reverse Proxy Configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 🚀 CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                           ?? new[] { "http://localhost:3000" }; // Default for React dev server
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 🌐 API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API Gateway", Version = "v1" });
    
    // Add JWT authentication to Swagger
    if (!string.IsNullOrEmpty(secretKey))
    {
        c.AddSecurityDefinition("Bearer", new()
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        
        c.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    }
});

// 🏥 Health Checks
var productServiceUrl = builder.Configuration["ProductServiceUrl"] ?? "http://localhost:5001";
var orderServiceUrl = builder.Configuration["OrderServiceUrl"] ?? "http://localhost:5002";
var authServiceUrl = builder.Configuration["AuthServiceUrl"] ?? "http://localhost:5003";

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri($"{productServiceUrl}/health"), "ProductService")
    .AddUrlGroup(new Uri($"{orderServiceUrl}/health"), "OrderService")
    .AddUrlGroup(new Uri($"{authServiceUrl}/health"), "AuthService");

var app = builder.Build();

// 🧱 Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1"));
}

// Global Exception Handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new { error = "An internal server error occurred in the API Gateway." };
        await context.Response.WriteAsJsonAsync(response);
    });
});

app.UseRouting();
app.UseCors("AllowFrontend");

if (!string.IsNullOrEmpty(secretKey))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// 🏥 Health check endpoint
app.MapHealthChecks("/health");

// 🏠 Gateway status endpoint
app.MapGet("/", () => Results.Ok(new { 
    message = "API Gateway is running...",
    timestamp = DateTime.UtcNow,
    services = new {
        productService = productServiceUrl,
        orderService = orderServiceUrl,
        authService = authServiceUrl
    }
}));

// 🔄 YARP Reverse Proxy - This handles all the routing automatically
app.MapReverseProxy();

app.Run();
