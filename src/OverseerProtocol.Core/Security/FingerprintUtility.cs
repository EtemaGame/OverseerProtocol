using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OverseerProtocol.Core.Security;

public static class FingerprintUtility
{
    public static string ComputeTextHash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value ?? "");
        return ToHex(sha.ComputeHash(bytes));
    }

    public static string ComputeFileHash(string path)
    {
        if (!File.Exists(path))
            return "";

        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return ToHex(sha.ComputeHash(stream));
    }

    public static string ComputeCombinedFileHash(IEnumerable<string> paths)
    {
        var builder = new StringBuilder();

        foreach (var path in paths)
        {
            builder.Append(path);
            builder.Append(':');
            builder.Append(ComputeFileHash(path));
            builder.AppendLine();
        }

        return ComputeTextHash(builder.ToString());
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
            builder.Append(value.ToString("x2"));

        return builder.ToString();
    }
}
