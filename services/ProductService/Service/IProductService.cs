using ProductService.Models;
public interface IProductService
{
    Task<List<Product>> GetAllASync();
    Task<Product> GetByIdASync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(int id); // novo método
}