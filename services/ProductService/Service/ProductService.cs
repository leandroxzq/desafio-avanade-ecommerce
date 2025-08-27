using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

public class ProductAppService : IProductService
{
    private readonly ProductDbContext _db;

    public ProductAppService(ProductDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllASync() =>
        await _db.Products.ToListAsync();

    public async Task<Product> GetByIdASync(int id) =>
        await _db.Products.FindAsync(id);

    public async Task<Product> CreateAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product != null)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
        }
    }
}
