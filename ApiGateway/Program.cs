using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register services BEFORE Build()
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // <-- moved here

var configuration = builder.Configuration;
var productServiceUrl = configuration["ProductServiceUrl"];
var orderServiceUrl = configuration["OrderServiceUrl"];

// Log the URLs
Console.WriteLine($"ProductServiceUrl: {productServiceUrl}");
Console.WriteLine($"OrderServiceUrl: {orderServiceUrl}");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "API Gateway is running...");

app.MapGet("/products", async (HttpClient client) =>
{
    var response = await client.GetAsync($"{productServiceUrl}/products");
    return Results.Content(await response.Content.ReadAsStringAsync(), "application/json");
});

app.MapGet("/orders", async (HttpClient client) =>
{
    var response = await client.GetAsync($"{orderServiceUrl}/orders");
    return Results.Content(await response.Content.ReadAsStringAsync(), "application/json");
});

app.Run();
