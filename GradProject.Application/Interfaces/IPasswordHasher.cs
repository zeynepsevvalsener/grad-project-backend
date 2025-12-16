namespace GradProject.Application.Interfaces
{
    public interface IPasswordHasher
    {
        (byte[] hash, byte[] salt) CreateHash(string password);
        bool Verify(string password, byte[] storedHash, byte[] storedSalt);
    }
}
