using System.Security.Cryptography;
using System.Text;

namespace NexodusAPI.Utils
{
    public class Cryptography
    {
        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256.
        /// </summary>
        /// <param name="password">Password to hash.</param>
        /// <returns>Hashed password string.</returns>
        public static string HashPassword(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // Generate a random 16-byte salt
                byte[] salt = new byte[16];
                rng.GetBytes(salt);

                // Generate the hash using PBKDF2
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(32); // 32-byte hash

                    // Combine salt + hash and convert to Base64
                    byte[] hashBytes = new byte[salt.Length + hash.Length];
                    Array.Copy(salt, 0, hashBytes, 0, salt.Length);
                    Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }

        /// <summary>
        /// Verifies a password against a stored hash.
        /// </summary>
        /// <param name="password">Password to verify.</param>
        /// <param name="storedHash">Real hashed password.</param>
        /// <returns>True if the entered password is correct.</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            // Extract the salt (first 16 bytes)
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, salt.Length);

            // Compute the hash of the provided password using the same salt
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32); // 32-byte hash

                // Compare byte-by-byte
                for (int i = 0; i < hash.Length; i++)
                {
                    if (hashBytes[salt.Length + i] != hash[i])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Generates a token for the user using HMACSHA256.
        /// </summary>
        /// <param name="email">email is used as a seed.</param>
        /// <returns>a token string.</returns>
        public static string GenerateToken(string email)
        {
            // Generate a token using HMACSHA256
            using (var hmac = new HMACSHA256())
            {
                byte[] tokenBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(email));
                return Convert.ToBase64String(tokenBytes);
            }
        }

        /// <summary>
        /// Decodes a base64 string.
        /// </summary>
        /// <param name="base64EncodedString">Base64 encoded string.</param>
        /// <returns>A string decoded from base64.</returns>
        public static string DecodeFromBase64(string base64EncodedString)
        {
            byte[] data = Convert.FromBase64String(base64EncodedString);
            return Encoding.UTF8.GetString(data);
        }
    }
}
