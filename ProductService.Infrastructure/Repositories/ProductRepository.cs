using Microsoft.EntityFrameworkCore;
using ProductService.Application.DTOs;
using Shared.Common.Responses;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;

        public ProductRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);

            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<Product>> GetAllAsync(ProductQueryParameters query)
        {
            IQueryable<Product> products = _context.Products.AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                products = products.Where(p =>
                   EF.Functions.Like(p.Name, $"%{query.Search}%"));
            }

            // Count BEFORE pagination
            var totalCount = await products.CountAsync();

            // Sorting
            products = query.SortBy?.ToLower() switch
            {
                "price" => query.Desc
                    ? products.OrderByDescending(p => p.Price)
                    : products.OrderBy(p => p.Price),

                "name" => query.Desc
                    ? products.OrderByDescending(p => p.Name)
                    : products.OrderBy(p => p.Name),

                _ => products.OrderBy(p => p.Name)
            };

            // Pagination
            var items = await products
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
        }
    }
}