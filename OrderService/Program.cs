using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Endpoints;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5240"); // <-- replace PORT with your ProductService port
});

builder.Services.AddDbContext<OrderServiceContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapOrdersEndpoints();

app.Run();
