using GradProject.Application.Interfaces;

namespace GradProject.Api.Middlewares
{
    public class RequestLanguageMiddleware : IMiddleware
    {
        private readonly ILanguageResolver _resolver;
        private readonly ICurrentLanguage _currentLanguage;

        public RequestLanguageMiddleware(ILanguageResolver resolver, ICurrentLanguage currentLanguage)
        {
            _resolver = resolver;
            _currentLanguage = currentLanguage;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var jwtLang = context.User?.FindFirst("lang")?.Value;
            var headerLang = context.Request.Headers["Accept-Language"].ToString();

            _currentLanguage.Value = _resolver.ResolveLanguage(jwtLang, headerLang);

            return next(context);
        }
    }
}