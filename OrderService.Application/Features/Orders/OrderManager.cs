using MapsterMapper;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using Shared.Common.Exceptions;
using OrderService.Application.Messaging.Events;
using OrderService.Application.Messaging.Interfaces;

namespace OrderService.Application.Features.Orders
{
    public class OrderManager
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductServiceClient _productServiceClient;
        private readonly IMapper _mapper;
        private readonly IEventBus _eventBus;

        public OrderManager(
            IOrderRepository orderRepository,
            IProductServiceClient productServiceClient,
            IMapper mapper,
            IEventBus eventBus)
        {
            _orderRepository = orderRepository;
            _productServiceClient = productServiceClient;
            _mapper = mapper;
            _eventBus = eventBus;
        }

        public async Task<OrderResponse> CreateAsync(CreateOrderRequest request)
        {
            var product = await _productServiceClient
                .GetProductAsync(request.ProductId);

            if (product == null)
            {
                throw new NotFoundException("Product not found");
            }

            var stockResult = await _productServiceClient
                .ReduceStockAsync(request.ProductId, request.Quantity);

            if (stockResult == null)
            {
                throw new Exception("Product service unavailable");
            }

            if (!stockResult.Success)
            {
                if (stockResult.Message == "Product not found")
                {
                    throw new NotFoundException(stockResult.Message);
                }

                throw new BadRequestException(stockResult.Message);
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                TotalPrice = product.Price * request.Quantity
            };

            await _orderRepository.AddAsync(order);

            await _eventBus.PublishAsync(
    "order-created",
    new OrderCreatedEvent
    {
        OrderId = order.Id,
        ProductId = order.ProductId,
        UserId = order.UserId,
        Quantity = order.Quantity,
        TotalPrice = order.TotalPrice,
        CreatedAt = order.CreatedAt
    });

            return _mapper.Map<OrderResponse>(order);
        }

        public async Task<List<OrderResponse>> GetAllAsync()
        {
            var orders = await _orderRepository.GetAllAsync();

            return _mapper.Map<List<OrderResponse>>(orders);
        }

        public async Task<OrderResponse?> GetByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null)
                return null;

            return _mapper.Map<OrderResponse>(order);
        }

        public async Task<OrderResponse?> UpdateStatusAsync(
            Guid id,
            UpdateOrderStatusRequest request)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null)
                return null;

            if (!IsValidStatusTransition(order.Status, request.Status))
            {
                throw new BadRequestException(
                    $"Cannot change status from {order.Status} to {request.Status}");
            }

            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);

            return _mapper.Map<OrderResponse>(order);
        }

        private static bool IsValidStatusTransition(
            OrderStatus current,
            OrderStatus next)
        {
            return current switch
            {
                OrderStatus.Pending =>
                    next == OrderStatus.Paid ||
                    next == OrderStatus.Cancelled,

                OrderStatus.Paid =>
                    next == OrderStatus.Shipped ||
                    next == OrderStatus.Cancelled,

                OrderStatus.Shipped =>
                    next == OrderStatus.Delivered,

                OrderStatus.Delivered => false,

                OrderStatus.Cancelled => false,

                _ => false
            };
        }
    }
}