using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddOpenApi();

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

// CRUD Endpoints

app.MapGet("/products", async (ProductDbContext db) =>
        await db.Products.ToListAsync()
).WithName("GetAllProducts");

app.MapGet("/products/{id:int}", async (int id, ProductDbContext db) =>
    await db.Products.FindAsync(id) is Product product
        ? Results.Ok(product)
        : Results.NotFound()
);

app.MapPost("/products", async (Product product, ProductDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
});

app.MapPut("/products/{id:int}", async (int id, Product inputProduct, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = inputProduct.Name;
    product.Description = inputProduct.Description;
    product.Price = inputProduct.Price;
    product.Stock = inputProduct.Stock;

    await db.SaveChangesAsync();
    return Results.NoContent();
});


app.MapDelete("/products/{id:int}", async (int id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

    db.Database.Migrate();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Notebook", Description = "Notebook Gamer", Price = 5000, Stock = 10 },
            new Product { Name = "Mouse", Description = "Mouse sem fio", Price = 150, Stock = 50 },
            new Product { Name = "Teclado", Description = "Teclado mecânico", Price = 350, Stock = 30 }
        );
        db.SaveChanges();
    }
}

app.Run();