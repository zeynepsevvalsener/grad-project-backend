using GradProject.Application.DTOs.Chat;
using GradProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [Route("api/v1/chat")]
    [Authorize]
    public class ChatController : ApiControllerBase
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

    }
}