using DiscountAndOrdering.Application.Dtos;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Application.Services;

public sealed class ProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var product = new Product(Guid.NewGuid(), request.Name, request.Description ?? string.Empty, request.Price, request.StockQuantity);
        await _productRepository.AddAsync(product);
        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, CreateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            return null;

        product.UpdateDetails(request.Name, request.Description ?? string.Empty, request.Price, request.StockQuantity);
        await _productRepository.UpdateAsync(product);
        return ToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            return false;

        await _productRepository.DeleteAsync(id);
        return true;
    }

    private static ProductDto ToDto(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product is null ? null : ToDto(product);
    }
}
