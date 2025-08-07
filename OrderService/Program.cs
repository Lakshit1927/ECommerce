using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Data;
using OrderService.Endpoints;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Database Configuration
builder.Services.AddDbContext<OrderServiceContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("OrdersDb");
    }
    else
    {
        // For production, use a real database
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString);
    }
});

// 🔐 JWT Authentication Configuration
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

// 🌐 HTTP Client for ProductService
var productServiceUrl = builder.Configuration["ProductServiceUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

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
    c.SwaggerDoc("v1", new() { Title = "Order Service API", Version = "v1" });
    
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
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderServiceContext>()
    .AddUrlGroup(new Uri($"{productServiceUrl}/health"), "ProductService");

var app = builder.Build();

// 🧱 Middleware Pipeline (Correct Order!)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1"));
}

// Global Exception Handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new { error = "An internal server error occurred." };
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

// 📍 API Endpoints
app.MapOrdersEndpoints();

app.Run();

