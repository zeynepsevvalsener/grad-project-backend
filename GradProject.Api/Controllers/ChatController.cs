using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Chat;
using GradProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponseDto>> SendMessage(
            ChatRequestDto request, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var response = await _chatService.SendMessageAsync(userId, request, ct);
            return Ok(response);
        }

        private int GetUserIdOrThrow()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");
            return userId;
        }
    }
}