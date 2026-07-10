using FluentAssertions;
using MapsterMapper;
using Moq;
using ProductService.Application.DTOs;
using ProductService.Application.DTOs.ProductService.Application.DTOs;
using ProductService.Application.Features.Products;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using Shared.Common.Responses;
using Xunit;
using Microsoft.Extensions.Caching.Distributed;


namespace ProductService.Tests.Features
{
    public class ProductManagerTests
    {
        private readonly Mock<IProductRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ProductManager _manager;
        private readonly Mock<IDistributedCache> _cacheMock;

        public ProductManagerTests()
        {
            _repositoryMock = new Mock<IProductRepository>();

            _mapperMock = new Mock<IMapper>();

            _cacheMock = new Mock<IDistributedCache>();

            _manager = new ProductManager(
                _repositoryMock.Object,
                _mapperMock.Object,
                _cacheMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateProductSuccessfully()
        {
            // Arrange

            var request = new CreateProductRequest
            {
                Name = "Laptop",
                Description = "Gaming Laptop",
                Price = 1000,
                StockQuantity = 5
            };

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            var response = new ProductResponse
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            _mapperMock
                .Setup(x => x.Map<Product>(request))
                .Returns(product);

            _mapperMock
                .Setup(x => x.Map<ProductResponse>(It.IsAny<Product>()))
                .Returns(response);

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            // Act

            var result = await _manager.CreateAsync(request);

            // Assert

            result.Should().NotBeNull();

            result.Name.Should().Be(request.Name);

            result.Price.Should().Be(request.Price);

            _repositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Product>()),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExists()
        {
            // Arrange
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Laptop",
                Description = "Gaming Laptop",
                Price = 1500,
                StockQuantity = 10
            };

            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _mapperMock
                .Setup(x => x.Map<ProductResponse>(product))
                .Returns(response);

            // Act
            var result = await _manager.GetByIdAsync(productId);

            // Assert
            result.Should().NotBeNull();

            result!.Id.Should().Be(productId);

            result.Name.Should().Be("Laptop");

            _repositoryMock.Verify(
                x => x.GetByIdAsync(productId),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
        {
            // Arrange
            var productId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _manager.GetByIdAsync(productId);

            // Assert
            result.Should().BeNull();

            _repositoryMock.Verify(
                x => x.GetByIdAsync(productId),
                Times.Once);
        }

        [Fact]
        public async Task ReduceStockAsync_ShouldReduceStockSuccessfully()
        {
            // Arrange
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Laptop",
                Price = 1000,
                StockQuantity = 10
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _manager.ReduceStockAsync(productId, 3);

            // Assert
            result.Should().NotBeNull();

            result.Success.Should().BeTrue();

            result.Message.Should().Be("Stock reduced successfully");

            product.StockQuantity.Should().Be(7);

            _repositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Product>()),
                Times.Once);
        }

        [Fact]
        public async Task ReduceStockAsync_ShouldReturnProductNotFound()
        {
            // Arrange
            var productId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _manager.ReduceStockAsync(productId, 3);

            // Assert
            result.Should().NotBeNull();

            result.Success.Should().BeFalse();

            result.Message.Should().Be("Product not found");

            _repositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Product>()),
                Times.Never);
        }

        [Fact]
        public async Task ReduceStockAsync_ShouldReturnInsufficientStock()
        {
            // Arrange
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Laptop",
                Price = 1000,
                StockQuantity = 2
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);

            // Act
            var result = await _manager.ReduceStockAsync(productId, 5);

            // Assert
            result.Should().NotBeNull();

            result.Success.Should().BeFalse();

            result.Message.Should().Be("Insufficient stock");

            // Stock should remain unchanged
            product.StockQuantity.Should().Be(2);

            // Database should not be updated
            _repositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Product>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateProductSuccessfully()
        {
            // Arrange
            var productId = Guid.NewGuid();

            var request = new UpdateProductRequest
            {
                Name = "Updated Laptop",
                Description = "Updated Gaming Laptop",
                Price = 1500,
                StockQuantity = 8
            };

            var product = new Product
            {
                Id = productId,
                Name = "Old Laptop",
                Description = "Old Description",
                Price = 1000,
                StockQuantity = 5
            };

            var response = new ProductResponse
            {
                Id = productId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);

            // Simulate Mapster copying values into the existing entity
            _mapperMock
                .Setup(x => x.Map(request, product))
                .Callback(() =>
                {
                    product.Name = request.Name;
                    product.Description = request.Description;
                    product.Price = request.Price;
                    product.StockQuantity = request.StockQuantity;
                });

            _repositoryMock
                .Setup(x => x.UpdateAsync(product))
                .Returns(Task.CompletedTask);

            _mapperMock
                .Setup(x => x.Map<ProductResponse>(product))
                .Returns(response);

            // Act
            var result = await _manager.UpdateAsync(productId, request);

            // Assert
            result.Should().NotBeNull();

            result!.Name.Should().Be(request.Name);

            result.Price.Should().Be(request.Price);

            result.StockQuantity.Should().Be(request.StockQuantity);

            // Verify entity was actually updated
            product.Name.Should().Be(request.Name);
            product.Price.Should().Be(request.Price);
            product.StockQuantity.Should().Be(request.StockQuantity);

            _repositoryMock.Verify(
                x => x.UpdateAsync(product),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenProductNotFound()
        {
            // Arrange
            var productId = Guid.NewGuid();

            var request = new UpdateProductRequest
            {
                Name = "Updated Laptop",
                Description = "Updated Description",
                Price = 1500,
                StockQuantity = 10
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _manager.UpdateAsync(productId, request);

            // Assert
            result.Should().BeNull();

            _repositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Product>()),
                Times.Never);

            _mapperMock.Verify(
                x => x.Map(request, It.IsAny<Product>()),
                Times.Never);

            _mapperMock.Verify(
                x => x.Map<ProductResponse>(It.IsAny<Product>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteProductSuccessfully()
        {
            // Arrange
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Laptop"
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _repositoryMock
                .Setup(x => x.DeleteAsync(product))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _manager.DeleteAsync(productId);

            // Assert
            result.Should().BeTrue();

            _repositoryMock.Verify(
                x => x.DeleteAsync(product),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenProductNotFound()
        {
            // Arrange
            var productId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _manager.DeleteAsync(productId);

            // Assert
            result.Should().BeFalse();

            _repositoryMock.Verify(
                x => x.DeleteAsync(It.IsAny<Product>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedProducts()
        {
            // Arrange
            var query = new ProductQueryParameters
            {
                Page = 1,
                PageSize = 10
            };

            var products = new List<Product>
    {
        new Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            Description = "Gaming",
            Price = 1000,
            StockQuantity = 5
        },
        new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mouse",
            Description = "Wireless",
            Price = 100,
            StockQuantity = 20
        }
    };

            var responses = new List<ProductResponse>
    {
        new ProductResponse
        {
            Id = products[0].Id,
            Name = products[0].Name,
            Description = products[0].Description,
            Price = products[0].Price,
            StockQuantity = products[0].StockQuantity
        },
        new ProductResponse
        {
            Id = products[1].Id,
            Name = products[1].Name,
            Description = products[1].Description,
            Price = products[1].Price,
            StockQuantity = products[1].StockQuantity
        }
    };

            var pagedResult = new PagedResult<Product>
            {
                Items = products,
                TotalCount = 2
            };

            _repositoryMock
                .Setup(x => x.GetAllAsync(query))
                .ReturnsAsync(pagedResult);

            _mapperMock
                .Setup(x => x.Map<List<ProductResponse>>(products))
                .Returns(responses);

            // Act
            var result = await _manager.GetAllAsync(query);

            // Assert
            result.Should().NotBeNull();

            result.Items.Should().HaveCount(2);

            result.TotalCount.Should().Be(2);

            result.Page.Should().Be(1);

            result.PageSize.Should().Be(10);

            result.Items[0].Name.Should().Be("Laptop");

            result.Items[1].Name.Should().Be("Mouse");

            _repositoryMock.Verify(
                x => x.GetAllAsync(query),
                Times.Once);
        }

    }
}
