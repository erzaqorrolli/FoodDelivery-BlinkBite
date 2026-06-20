using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FoodDeliveryyy.Middleware
{
    public class GlobalExceptionHandlerMiddleware : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = GetStatusCode(exception),
                Title = "An error occurred while processing your request",
                Type = exception.GetType().Name, 
                Instance = httpContext.Request.Path
            };

            if (_env.IsDevelopment())
            {
                problemDetails.Detail = exception.Message;
                problemDetails.Extensions["stackTrace"] = exception.StackTrace?.ToString();
            }
            else
            {
                problemDetails.Detail = "An internal error occurred. Please try again later.";
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await httpContext.Response.WriteAsync(json, cancellationToken);

            return true;
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                InvalidOperationException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}

