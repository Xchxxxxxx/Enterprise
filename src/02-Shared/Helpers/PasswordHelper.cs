using System.Security.Cryptography;

namespace EfCore.Enterprise.Shared.Helpers;

public static class PasswordHelper
{
    private const int SaltSize = 32;
    private const int HashSize = 64;
    private const int Iterations = 100000;

    public static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        var hash = Convert.ToBase64String(hashBytes);
        var salt = Convert.ToBase64String(saltBytes);

        return (hash, salt);
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var expectedHash = Convert.FromBase64String(hash);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}