using System.Security.Cryptography;

namespace HotelManagement.Web.Services;

/// <summary>
/// Băm mật khẩu bằng PBKDF2-SHA256 — NFR-02. Không bao giờ lưu mật khẩu gốc.
/// Định dạng chuỗi lưu trong Employee.PasswordHash: {iterations}.{salt-base64}.{hash-base64}
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        // So sánh theo thời gian cố định để tránh tấn công đo thời gian.
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
