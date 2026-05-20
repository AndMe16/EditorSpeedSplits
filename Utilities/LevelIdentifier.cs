using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EditorSpeedSplits.Utilities;

internal static class LevelIdentifier
{
    internal static string MakeLevelIdentifier(string levelPath)
    {
        var shortName = Path.GetFileNameWithoutExtension(levelPath);
        var hash = ComputeHash(levelPath);
        return $"{shortName}_{hash}";
    }

    private static string ComputeHash(string input)
    {
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha1.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes)
            .Replace("-", "")[..8]
            .ToLowerInvariant();
    }
}