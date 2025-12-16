using GradProject.Domain.Entities;

namespace GradProject.Application.Interfaces
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAtUtc) CreateToken(User user);
    }
}
