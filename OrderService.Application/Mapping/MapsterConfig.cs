using Mapster;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mapping
{
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<Order, OrderResponse>
                .NewConfig();

            TypeAdapterConfig<CreateOrderRequest, Order>
                .NewConfig();

            TypeAdapterConfig<UpdateOrderStatusRequest, Order>
                .NewConfig();
        }
    }
}