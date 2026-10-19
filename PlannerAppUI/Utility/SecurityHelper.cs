using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PlannerApplication.Utility
{
    public static class SecurityHelper
    {
        /// <summary>
        /// Computes a standard SHA-256 hash of the input string.
        /// </summary>
        public static string HashSecret(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// Verifies a secret against a stored hash (or legacy plaintext for backward compatibility).
        /// </summary>
        public static bool VerifySecret(string input, string storedHashOrPlaintext)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(storedHashOrPlaintext))
            {
                return false;
            }

            string inputHash = HashSecret(input);
            if (string.Equals(inputHash, storedHashOrPlaintext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback for pre-existing plaintext records in legacy database files
            return input == storedHashOrPlaintext;
        }

        /// <summary>
        /// Checks if a string has the format of a 64-character hex SHA-256 hash.
        /// </summary>
        public static bool IsHashed(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length != 64) return false;
            return input.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        /// <summary>
        /// Generates a cryptographically secure 6-digit numeric recovery code (000000 - 999999).
        /// Properly preserves leading zeros and ensures uniqueness against existing hashes.
        /// </summary>
        public static string GenerateRecoveryCode(IEnumerable<string>? existingHashes = null)
        {
            var hashSet = existingHashes != null
                ? new HashSet<string>(existingHashes, StringComparer.OrdinalIgnoreCase)
                : null;

            while (true)
            {
                int num = RandomNumberGenerator.GetInt32(0, 1000000);
                string code = num.ToString("D6"); // Always exactly 6 digits with leading zeros
                string hash = HashSecret(code);

                if (hashSet == null || !hashSet.Contains(hash))
                {
                    return code;
                }
            }
        }
    }
}
