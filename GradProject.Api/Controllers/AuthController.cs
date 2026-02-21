using FluentValidation;
using GradProject.Application.DTOs.Auth;
using GradProject.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterRequestDto> _registerValidator;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly ILocalizationService _localization;

        public AuthController(
            IAuthService authService,
            IValidator<RegisterRequestDto> registerValidator,
            IValidator<LoginRequestDto> loginValidator,
            ILocalizationService localization)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _localization = localization;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
        {
            var validation = await _registerValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .Select(e => new
                    {
                        field = e.PropertyName,
                        message = _localization.Get(e.ErrorMessage)
                    });

                return BadRequest(errors);
            }

            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
        {
            var validation = await _loginValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .Select(e => new
                    {
                        field = e.PropertyName,
                        message = _localization.Get(e.ErrorMessage)
                    });

                return BadRequest(errors);
            }

            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
    }
}
