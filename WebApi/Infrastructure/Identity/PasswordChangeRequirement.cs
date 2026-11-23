using System.Security.Cryptography;

namespace WebApi.Infrastructure.Identity;

// A user with this claim (stored in AspNetUserClaims and copied into the JWT) may only change their password.
public static class PasswordChangeRequirement
{
    public const string ClaimType = "must_change_password";
    public const string ClaimValue = "true";

    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const int TemporaryPasswordLength = 12;

    // Satisfies the Identity password policy (length, digit, lowercase); ambiguous characters are left out.
    public static string GenerateTemporaryPassword()
    {
        string all = Lowercase + Uppercase + Digits;
        char[] password = new char[TemporaryPasswordLength];

        password[0] = Lowercase[RandomNumberGenerator.GetInt32(Lowercase.Length)];
        password[1] = Uppercase[RandomNumberGenerator.GetInt32(Uppercase.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];

        for (int i = 3; i < password.Length; i++)
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        RandomNumberGenerator.Shuffle(password.AsSpan());
        return new string(password);
    }
}
