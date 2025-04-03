using System.Security.Cryptography;

namespace NexodusAPI.Utils
{
    public class Password
    {
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
    }
}
