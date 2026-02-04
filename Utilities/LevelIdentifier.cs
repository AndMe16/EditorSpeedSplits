using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EditorSpeedSplits.Utilities
{
    internal static class LevelIdentifier
    {
        internal static string MakeLevelIdentifier(string levelPath)
        {
            string shortName = Path.GetFileNameWithoutExtension(levelPath);
            string hash = ComputeHash(levelPath);
            return $"{shortName}_{hash}";
        }

        private static string ComputeHash(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes)
                    .Replace("-", "")
                    .Substring(0, 8)
                    .ToLowerInvariant();
            }
        }
    }
}
