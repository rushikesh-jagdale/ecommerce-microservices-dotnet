using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.DTOs.ProductService.Application.DTOs;
using ProductService.Application.Features.Products;
using Microsoft.AspNetCore.Authorization;

namespace ProductService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductManager _productService;

        public ProductsController(ProductManager productService)
        {
            _productService = productService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            CreateProductRequest request)
        {
            var result = await _productService.CreateAsync(request);

            return Ok(result);
        }

        // Updated
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] ProductQueryParameters query)
        {
            var result = await _productService.GetAllAsync(query);

            return Ok(result);
        }

        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _productService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("{id}/reduce-stock")]
        public async Task<IActionResult> ReduceStock(
            Guid id,
            [FromQuery] int quantity)
        {
            var result = await _productService.ReduceStockAsync(id, quantity);

            if (!result.Success)
            {
                if (result.Message == "Product not found")
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateProductRequest request)
        {
            var result = await _productService.UpdateAsync(id, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _productService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}