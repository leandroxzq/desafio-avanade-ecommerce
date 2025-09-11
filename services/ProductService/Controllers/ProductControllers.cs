using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.DTOs;
using ProductService.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _productService.GetAllASync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdASync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        var userId =  User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        product.CreatedBy = userId;

        var created = await _productService.CreateAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Product input)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existing = await _productService.GetByIdASync(id);
        if (existing is null || existing.CreatedBy != userId)
        {

            return NotFound();
        }

        existing.Name = input.Name;
        existing.Description = input.Description;
        existing.Price = input.Price;
        existing.Stock = input.Stock;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _productService.UpdateAsync(existing);
        return Ok(updated);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var product = await _productService.GetByIdASync(id);
        if (product is null || product.CreatedBy != userId)
        {
            return NotFound();
        }

        await _productService.DeleteAsync(product.Id);

        return NoContent();
    }

    [HttpPost("availability")]
    public async Task<IActionResult> CheckAvaibility([FromBody] List<ProductStockRequest> items)
    {
        var missing = new List<UnavailableItem>();

        foreach (var i in items)
        {
            var product = await _productService.GetByIdASync(i.ProductId);
            if (product == null || product.Stock < i.Quantity)
            {
                missing.Add(new UnavailableItem(i.ProductId, product?.Stock ?? 0));
            }
        }

        return Ok(new AvailabilityResponse
        {
            Available = missing.Count == 0,
            Missing = missing
        });
    }

    [HttpPost("decrease")]
    public async Task<IActionResult> DecreaseStock([FromBody] List<ProductStockRequest> items)
    {
        var failed = new List<UnavailableItem>();

        foreach (var i in items)
        {
            var product = await _productService.GetByIdASync(i.ProductId);
            if (product == null || product.Stock < i.Quantity)
            {
                failed.Add(new UnavailableItem(i.ProductId, product?.Stock ?? 0));
                continue;
            }

            product.Stock -= i.Quantity;
            await _productService.UpdateAsync(product);
        }

        return Ok(new DecreaseResponse
        {
            Success = failed.Count == 0,
            Failed = failed
        });
    }
}
