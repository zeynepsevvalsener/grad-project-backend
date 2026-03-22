using System.Text.Json;
using System.Text.Json.Serialization;
using GradProject.Api.Models;
using GradProject.Application.Interfaces;

namespace GradProject.Api.Middlewares
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ICurrentLanguage _currentLanguage;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            ICurrentLanguage currentLanguage,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _currentLanguage = currentLanguage;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access");
                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (InvalidOperationException ex) when (ex.Message == "RUN_NOT_FOUND")
            {
                _logger.LogWarning(ex, "Resource not found (RUN_NOT_FOUND)");
                await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (InvalidOperationException ex) when (IsNotFoundStyleInvalidOperation(ex))
            {
                _logger.LogWarning(ex, "Resource not found");
                await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Badge must be loaded", StringComparison.Ordinal))
            {
                _logger.LogError(ex, "Invariant violation in badge loading");
                await WriteServerErrorAsync(context, ex);
            }
            catch (InvalidOperationException ex) when (IsClientBadRequestInvalidOperation(ex))
            {
                _logger.LogWarning(ex, "Bad request (business validation)");
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflict or business rule violation");
                await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found");
                await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Bad request (null argument)");
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message ?? "A required value was missing.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Bad request (argument)");
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteServerErrorAsync(context, ex);
            }
        }

        private static bool IsNotFoundStyleInvalidOperation(InvalidOperationException ex)
        {
            var m = ex.Message;
            return m.Contains("Profile not found", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsClientBadRequestInvalidOperation(InvalidOperationException ex)
        {
            var m = ex.Message;
            if (m.Contains("TDEE", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.Contains("BMR", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.StartsWith("Height must", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.Contains("date of birth", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.Contains("set your height", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.Contains("set your weight", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.Contains("set your gender", StringComparison.OrdinalIgnoreCase)) return true;
            if (m.Contains("not valid for TDEE", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private async Task WriteServerErrorAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var isDevelopment = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;

            var msg = _currentLanguage.Value == "tr"
                ? "Beklenmeyen bir hata oluştu."
                : "An unexpected error occurred.";

            var traceId = GetTraceId(context);
            string? detail = null;
            if (isDevelopment)
            {
                detail = ex.Message;
                if (ex.StackTrace != null)
                    detail += Environment.NewLine + ex.StackTrace;
                if (ex.InnerException != null)
                    detail += Environment.NewLine + "Inner: " + ex.InnerException.Message;
            }

            var body = new ApiErrorResponse
            {
                Message = msg,
                TraceId = traceId,
                Detail = detail
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }

        private async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var body = new ApiErrorResponse
            {
                Message = message,
                TraceId = GetTraceId(context)
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }

        private static string? GetTraceId(HttpContext context)
        {
            return context.Response.Headers.TryGetValue("X-Correlation-Id", out var fromResponse)
                ? fromResponse.FirstOrDefault()
                : context.Request.Headers.TryGetValue("X-Correlation-Id", out var fromRequest)
                    ? fromRequest.FirstOrDefault()
                    : context.TraceIdentifier;
        }
    }
}
