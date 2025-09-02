using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddOpenApi();
builder.Services.AddScoped<IProductService, ProductAppService>();
builder.Services.AddControllers();

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

    db.Database.Migrate();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Notebook", Description = "Notebook Gamer", Price = 5000, Stock = 10, CreatedBy = "admin" },
            new Product { Name = "Mouse", Description = "Mouse sem fio", Price = 150, Stock = 50, CreatedBy = "admin" },
            new Product { Name = "Teclado", Description = "Teclado mecânico", Price = 350, Stock = 30, CreatedBy = "admin" }
        );
        db.SaveChanges();
    }
}

app.Run();