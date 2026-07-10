using FluentAssertions;
using MapsterMapper;
using Moq;
using OrderService.Application.DTOs;
using OrderService.Application.Features.Orders;
using OrderService.Application.Interfaces;
using OrderService.Application.Messaging.Events;
using OrderService.Application.Messaging.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using Shared.Common.Exceptions;
using Xunit;


namespace OrderService.Tests.Features
{
    public class OrderManagerTests
    {
        private readonly Mock<IOrderRepository> _repositoryMock;

        private readonly Mock<IProductServiceClient> _productServiceMock;

        private readonly Mock<IMapper> _mapperMock;

        private readonly OrderManager _manager;

        private readonly Mock<IEventBus> _eventBusMock;

        public OrderManagerTests()
        {
            _repositoryMock = new Mock<IOrderRepository>();

            _productServiceMock = new Mock<IProductServiceClient>();

            _eventBusMock = new Mock<IEventBus>();

            _mapperMock = new Mock<IMapper>();

            _manager = new OrderManager(
                _repositoryMock.Object,
                _productServiceMock.Object,
                _mapperMock.Object,
                _eventBusMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateOrderSuccessfully()
        {
            // Arrange

            var request = new CreateOrderRequest
            {
                UserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 2
            };

            var product = new ProductDto
            {
                Id = request.ProductId,
                Name = "Laptop",
                Price = 1000
            };

            var stockResult = new StockResult
            {
                Success = true,
                Message = "Stock reduced successfully"
            };

            _productServiceMock
                .Setup(x => x.GetProductAsync(request.ProductId))
                .ReturnsAsync(product);

            _productServiceMock
                .Setup(x => x.ReduceStockAsync(
                    request.ProductId,
                    request.Quantity))
                .ReturnsAsync(stockResult);

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mapperMock
                .Setup(x => x.Map<OrderResponse>(It.IsAny<Order>()))
                .Returns((Order order) => new OrderResponse
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status,
                    CreatedAt = order.CreatedAt
                });
            _eventBusMock
    .Setup(x => x.PublishAsync(
        It.IsAny<string>(),
        It.IsAny<OrderCreatedEvent>()))
    .Returns(Task.CompletedTask);

            // Act

            var result = await _manager.CreateAsync(request);

            // Assert

            result.Should().NotBeNull();

            result.UserId.Should().Be(request.UserId);

            result.ProductId.Should().Be(request.ProductId);

            result.Quantity.Should().Be(2);

            result.TotalPrice.Should().Be(2000);

            _productServiceMock.Verify(
                x => x.GetProductAsync(request.ProductId),
                Times.Once);

            _productServiceMock.Verify(
                x => x.ReduceStockAsync(
                    request.ProductId,
                    request.Quantity),
                Times.Once);

            _repositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Order>()),
                Times.Once);

            _mapperMock.Verify(
                x => x.Map<OrderResponse>(It.IsAny<Order>()),
                Times.Once);

            _eventBusMock.Verify(
        x => x.PublishAsync(
        "order-created",
        It.IsAny<OrderCreatedEvent>()),
        Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenProductDoesNotExist()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                UserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 2
            };

            _productServiceMock
                .Setup(x => x.GetProductAsync(request.ProductId))
                .ReturnsAsync((ProductDto?)null);

            _eventBusMock
    .Setup(x => x.PublishAsync(
        It.IsAny<string>(),
        It.IsAny<OrderCreatedEvent>()))
    .Returns(Task.CompletedTask);

            // Act
            Func<Task> act = async () =>
                await _manager.CreateAsync(request);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage("Product not found");

            _productServiceMock.Verify(
                x => x.ReduceStockAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>()),
                Times.Never);

            _repositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Order>()),
                Times.Never);

            _mapperMock.Verify(
                x => x.Map<OrderResponse>(It.IsAny<Order>()),
                Times.Never);

            _eventBusMock.Verify(
    x => x.PublishAsync(
        "order-created",
        It.IsAny<OrderCreatedEvent>()),
    Times.Once);

        }
        [Fact]
        public async Task CreateAsync_ShouldThrowBadRequestException_WhenStockIsInsufficient()
        {
            // Arrange

            var request = new CreateOrderRequest
            {
                UserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 5
            };

            var product = new ProductDto
            {
                Id = request.ProductId,
                Name = "Laptop",
                Price = 1000
            };

            var stockResult = new StockResult
            {
                Success = false,
                Message = "Insufficient stock"
            };

            _productServiceMock
                .Setup(x => x.GetProductAsync(request.ProductId))
                .ReturnsAsync(product);

            _productServiceMock
                .Setup(x => x.ReduceStockAsync(
                    request.ProductId,
                    request.Quantity))
                .ReturnsAsync(stockResult);

            _eventBusMock
    .Setup(x => x.PublishAsync(
        It.IsAny<string>(),
        It.IsAny<OrderCreatedEvent>()))
    .Returns(Task.CompletedTask);

            // Act

            Func<Task> act = async () =>
                await _manager.CreateAsync(request);

            // Assert

            await act.Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Insufficient stock");

            _repositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Order>()),
                Times.Never);

            _mapperMock.Verify(
                x => x.Map<OrderResponse>(It.IsAny<Order>()),
                Times.Never);

            _productServiceMock.Verify(
                x => x.GetProductAsync(request.ProductId),
                Times.Once);

            _productServiceMock.Verify(
                x => x.ReduceStockAsync(
                    request.ProductId,
                    request.Quantity),
                Times.Once);

            _eventBusMock.Verify(
    x => x.PublishAsync(
        "order-created",
        It.IsAny<OrderCreatedEvent>()),
    Times.Once);
        }
        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenProductServiceUnavailable()
        {
            // Arrange

            var request = new CreateOrderRequest
            {
                UserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 2
            };

            var product = new ProductDto
            {
                Id = request.ProductId,
                Name = "Laptop",
                Price = 1000
            };

            _productServiceMock
                .Setup(x => x.GetProductAsync(request.ProductId))
                .ReturnsAsync(product);

            _productServiceMock
                .Setup(x => x.ReduceStockAsync(
                    request.ProductId,
                    request.Quantity))
                .ReturnsAsync((StockResult?)null);

            _eventBusMock
    .Setup(x => x.PublishAsync(
        It.IsAny<string>(),
        It.IsAny<OrderCreatedEvent>()))
    .Returns(Task.CompletedTask);

            // Act

            Func<Task> act = async () =>
                await _manager.CreateAsync(request);

            // Assert

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Product service unavailable");

            _repositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Order>()),
                Times.Never);

            _eventBusMock.Verify(
    x => x.PublishAsync(
        "order-created",
        It.IsAny<OrderCreatedEvent>()),
    Times.Once);

        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnOrder()
        {
            // Arrange

            var orderId = Guid.NewGuid();

            var order = new Order
            {
                Id = orderId,
                UserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 2,
                TotalPrice = 2000
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            _mapperMock
                .Setup(x => x.Map<OrderResponse>(order))
                .Returns(new OrderResponse
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    TotalPrice = order.TotalPrice
                });

            // Act

            var result = await _manager.GetByIdAsync(orderId);

            // Assert

            result.Should().NotBeNull();

            result!.Id.Should().Be(orderId);

            result.TotalPrice.Should().Be(2000);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange

            var orderId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act

            var result = await _manager.GetByIdAsync(orderId);

            // Assert

            result.Should().BeNull();

            _mapperMock.Verify(
                x => x.Map<OrderResponse>(It.IsAny<Order>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnOrders()
        {
            // Arrange

            var orders = new List<Order>
    {
        new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Quantity = 1,
            TotalPrice = 1000
        },
        new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            TotalPrice = 3000
        }
    };

            var responses = new List<OrderResponse>
    {
        new OrderResponse
        {
            Id = orders[0].Id,
            UserId = orders[0].UserId,
            ProductId = orders[0].ProductId,
            Quantity = orders[0].Quantity,
            TotalPrice = orders[0].TotalPrice
        },
        new OrderResponse
        {
            Id = orders[1].Id,
            UserId = orders[1].UserId,
            ProductId = orders[1].ProductId,
            Quantity = orders[1].Quantity,
            TotalPrice = orders[1].TotalPrice
        }
    };

            _repositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(orders);

            _mapperMock
                .Setup(x => x.Map<List<OrderResponse>>(orders))
                .Returns(responses);

            // Act

            var result = await _manager.GetAllAsync();

            // Assert

            result.Should().HaveCount(2);

            result[0].TotalPrice.Should().Be(1000);

            result[1].TotalPrice.Should().Be(3000);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldUpdateStatusSuccessfully()
        {
            // Arrange

            var orderId = Guid.NewGuid();

            var order = new Order
            {
                Id = orderId,
                Status = OrderStatus.Pending
            };

            var request = new UpdateOrderStatusRequest
            {
                Status = OrderStatus.Paid
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            _repositoryMock
                .Setup(x => x.UpdateAsync(order))
                .Returns(Task.CompletedTask);

            _mapperMock
                .Setup(x => x.Map<OrderResponse>(order))
                .Returns(new OrderResponse
                {
                    Id = order.Id,
                    Status = OrderStatus.Paid
                });

            // Act

            var result = await _manager.UpdateStatusAsync(orderId, request);

            // Assert

            result.Should().NotBeNull();

            result!.Status.Should().Be(OrderStatus.Paid);

            _repositoryMock.Verify(
                x => x.UpdateAsync(order),
                Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowBadRequest_WhenTransitionIsInvalid()
        {
            // Arrange

            var orderId = Guid.NewGuid();

            var order = new Order
            {
                Id = orderId,
                Status = OrderStatus.Delivered
            };

            var request = new UpdateOrderStatusRequest
            {
                Status = OrderStatus.Pending
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act

            Func<Task> act = async () =>
                await _manager.UpdateStatusAsync(orderId, request);

            // Assert

            await act.Should()
                .ThrowAsync<BadRequestException>();

            _repositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Order>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange

            var orderId = Guid.NewGuid();

            var request = new UpdateOrderStatusRequest
            {
                Status = OrderStatus.Paid
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act

            var result = await _manager.UpdateStatusAsync(orderId, request);

            // Assert

            result.Should().BeNull();

            _repositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Order>()),
                Times.Never);
        }
    }
}