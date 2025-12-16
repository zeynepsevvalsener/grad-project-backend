using System.Security.Cryptography;
using System.Text;
using GradProject.Application.Interfaces;

namespace GradProject.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public (byte[] hash, byte[] salt) CreateHash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(32);

            using var hmac = new HMACSHA512(salt);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return (hash, salt);
        }

        public bool Verify(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (storedHash is null || storedHash.Length == 0) return false;
            if (storedSalt is null || storedSalt.Length == 0) return false;

            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }
}
