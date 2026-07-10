using Mapster;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Mapping
{
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<User, AuthResponse>
                .NewConfig();
        }
    }
}