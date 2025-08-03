using Productservice.Data;
using Microsoft.EntityFrameworkCore;
using ProductService.Endpoints;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseInMemoryDatabase("ProductsDb"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapProductEndpoints(); // 👈 VERY IMPORTANT

app.Run();
