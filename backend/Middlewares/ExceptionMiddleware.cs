using System.Net;
using System.Text.Json;

namespace backend.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            catch (ArgumentException ex)
            {
                //Bad input, validation failures — 400
                _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                //User is authenticated but doesn't have permission — 403
                //e.g. trying to edit someone else's item, or borrowing while blocked
                _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                //Resource not found in DB — 404
                //e.g. item ID doesn't exist, loan ID doesn't exist
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                //Business rule violations — 409 Conflict
                //e.g. trying to borrow an item that's already on loan,
                //trying to delete account with active loans,
                //Trying to borrow with score below 20
                _logger.LogWarning(ex, "Conflict: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Conflict, ex.Message);
            }

            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database error: {Message}", ex.InnerException?.Message ?? ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.BadRequest, "Invalid data. Please check your input.");
            }

            catch (Exception ex)
            {
                //Anything unexpected — 500
                //Log the full exception server-side always
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

                // Only expose real message in development — hide it in production
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var message = env.IsDevelopment()
                    ? ex.Message
                    : "An unexpected error occurred. Please try again later.";

                await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, message);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                Success = false,
                StatusCode = (int)statusCode,
                Message = message
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}