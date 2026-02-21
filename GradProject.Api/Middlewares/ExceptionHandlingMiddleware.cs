using System.Text.Json;
using GradProject.Application.Interfaces;

namespace GradProject.Api.Middlewares
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ICurrentLanguage _currentLanguage;

        public ExceptionHandlingMiddleware(ICurrentLanguage currentLanguage)
        {
            _currentLanguage = currentLanguage;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
            }
            catch (InvalidOperationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var isDevelopment = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;

                var msg = _currentLanguage.Value == "tr"
                    ? "Beklenmeyen bir hata oluştu."
                    : "An unexpected error occurred.";

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = msg,
                    detail = isDevelopment ? ex.Message : null,
                    stackTrace = isDevelopment ? ex.StackTrace : null,
                    innerException = isDevelopment && ex.InnerException != null ? ex.InnerException.Message : null
                }));
            }
        }
    }
}