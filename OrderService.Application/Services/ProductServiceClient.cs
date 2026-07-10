using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Application.Services
{
    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;

        public ProductServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductDto?> GetProductAsync(Guid productId)
        {
            var response = await _httpClient.GetAsync($"api/products/{productId}");

            Console.WriteLine($"Status Code: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Response:");
            Console.WriteLine(content);

            if (!response.IsSuccessStatusCode)
                return null;

            return System.Text.Json.JsonSerializer.Deserialize<ProductDto>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public async Task<StockResult?> ReduceStockAsync(
            Guid productId,
            int quantity)
        {
            var response = await _httpClient.PostAsync(
                $"api/products/{productId}/reduce-stock?quantity={quantity}",
                null);

            var result = await response.Content
                .ReadFromJsonAsync<StockResult>();

            return result;
        }


    }
}