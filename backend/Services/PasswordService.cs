using System.Security.Cryptography;
using System.Text;

namespace backend.Services
{
    public class PasswordService
    {
        private const int KeySize = 64;
        private const int Iterations = 350000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

        public string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(KeySize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySize);

            return string.Join(':', Convert.ToHexString(salt), Convert.ToHexString(hash));
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            var elements = passwordHash.Split(':');
            if (elements.Length != 2)
                return false;

            var salt = Convert.FromHexString(elements[0]);
            var hash = Convert.FromHexString(elements[1]);

            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(hash, hashToCompare);
        }
    }
} 