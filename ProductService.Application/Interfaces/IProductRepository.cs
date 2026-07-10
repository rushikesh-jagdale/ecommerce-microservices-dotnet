using ProductService.Application.DTOs;
using Shared.Common.Responses;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ProductService.Application.Interfaces
{
    public interface IProductRepository
    {
        Task AddAsync(Product product);

        Task<PagedResult<Product>> GetAllAsync(ProductQueryParameters query);

        Task<Product?> GetByIdAsync(Guid id);

        Task UpdateAsync(Product product);

        Task DeleteAsync(Product product);
    }
}
