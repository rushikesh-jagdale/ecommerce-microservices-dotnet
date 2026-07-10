using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order);
        Task<List<Order>> GetAllAsync();

        Task<Order?> GetByIdAsync(Guid id);
        Task UpdateAsync(Order order);
    }
}