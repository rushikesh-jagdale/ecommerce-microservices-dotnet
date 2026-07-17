using ProductService.Application.DTOs;
using ProductService.Application.DTOs.ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using Shared.Common.Responses;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Serilog;

namespace ProductService.Application.Features.Products
{
    public class ProductManager
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public ProductManager(
    IProductRepository productRepository,
    IMapper mapper,
    IDistributedCache cache)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ProductResponse> CreateAsync(
            CreateProductRequest request)
        {
            var product = _mapper.Map<Product>(request);
            product.Id = Guid.NewGuid();

            await _productRepository.AddAsync(product);

            return _mapper.Map<ProductResponse>(product);
        }

        // Updated
        public async Task<PagedResponse<ProductResponse>> GetAllAsync(
    ProductQueryParameters query)
        {
            var result = await _productRepository.GetAllAsync(query);

            var items = _mapper.Map<List<ProductResponse>>(result.Items);

            return new PagedResponse<ProductResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount,
                
            };
        }

        public async Task<ProductResponse?> GetByIdAsync(Guid id)
        {
            var cacheKey = $"product:{id}";

            // Try Redis first
            try
            {
                var cachedProduct = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedProduct))
                {
                    Log.Information("Cache HIT for {CacheKey}", cacheKey);

                    return JsonSerializer.Deserialize<ProductResponse>(cachedProduct);
                }

                Log.Information("Cache MISS for {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Redis unavailable. Falling back to PostgreSQL.");
            }

            // Always fallback to PostgreSQL
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
                return null;

            var response = _mapper.Map<ProductResponse>(product);

            // Try writing to Redis
            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(response),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                Log.Information("Product cached with key {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to cache product in Redis.");
            }

            return response;
        }

        public async Task<StockResult> ReduceStockAsync(
            Guid productId,
            int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                return new StockResult
                {
                    Success = false,
                    Message = "Product not found"
                };
            }

            if (product.StockQuantity < quantity)
            {
                return new StockResult
                {
                    Success = false,
                    Message = "Insufficient stock"
                };
            }

            product.StockQuantity -= quantity;

            await _productRepository.UpdateAsync(product);

            return new StockResult
            {
                Success = true,
                Message = "Stock reduced successfully"
            };
        }

        public async Task<ProductResponse?> UpdateAsync(
            Guid id,
            UpdateProductRequest request)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
                return null;

            _mapper.Map(request, product);

            await _productRepository.UpdateAsync(product);

            try
            {
                await _cache.RemoveAsync($"product:{id}");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to remove cache.");
            }

            Log.Information("Cache removed for product:{ProductId}", id);

            return _mapper.Map<ProductResponse>(product);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            await _productRepository.DeleteAsync(product);

            try
            {
                await _cache.RemoveAsync($"product:{id}");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to remove cache.");
            }

            Log.Information("Cache removed for product:{ProductId}", id);

            return true;
        }
    }
}