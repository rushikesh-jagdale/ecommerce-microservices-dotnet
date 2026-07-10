using Microsoft.Extensions.Logging;
using Shared.Common.Exceptions;
using Shared.Common.Responses;
using System.Text.Json;

namespace UserService.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                              ex,
                              "Exception occurred. Method: {Method}, Path: {Path}",
                               context.Request.Method,
                               context.Request.Path);

                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Data = null
                };

                switch (ex)
                {
                    case NotFoundException:
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        response.Message = ex.Message;
                        break;

                    case BadRequestException:
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        response.Message = ex.Message;
                        break;

                    default:
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = "Internal Server Error";
                        break;
                }

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response));
            }
        }
    }
}