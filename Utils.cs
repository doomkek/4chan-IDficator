using System.Security.Cryptography;
using System.Text;

public static class Utils
{
    //TODO: change hex into something more interesting, userscripts uses hex value as color
    public static string GenerateHash(long threadId, string token)
    {
        using SHA256 s = SHA256.Create();
        byte[] hashBytes = s.ComputeHash(Encoding.UTF8.GetBytes($"{threadId}_{token}"));
        byte[] truncatedHash = new byte[3];
        Array.Copy(hashBytes, truncatedHash, 3);

        return BitConverter.ToString(truncatedHash).Replace("-", string.Empty).ToLowerInvariant();
    }

    public static string GenerateToken(long threadId, string ip, string secret)
    {
        using SHA512 s = SHA512.Create();
        byte[] hashBytes = s.ComputeHash(Encoding.UTF8.GetBytes($"{threadId}_{ip}_{secret}"));
        byte[] truncatedHash = new byte[64];
        Array.Copy(hashBytes, truncatedHash, 64);

        return BitConverter.ToString(truncatedHash).Replace("-", string.Empty);
    }
}
   