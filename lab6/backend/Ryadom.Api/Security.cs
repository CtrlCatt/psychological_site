using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Ryadom.Application;

namespace Ryadom.Api;

// ASP.NET Identity PasswordHasher использует случайную соль и PBKDF2.
public sealed class StrongPasswordService : IPasswordService
{
    private readonly PasswordHasher<Account> hasher = new();

    public string Hash(Account account, string password) => hasher.HashPassword(account, password);

    public bool Verify(Account account, string password) =>
        hasher.VerifyHashedPassword(account, account.PasswordHash, password)
        is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}

public sealed class JwtTokenIssuer(IConfiguration configuration, TimeProvider time) : IAccessTokenIssuer
{
    public AuthSession Issue(Account account)
    {
        var now = time.GetUtcNow();
        var expires = now.AddMinutes(60);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString("D")),
            new Claim("unique_name", account.UserName),
            new Claim("role", account.Role)
        };
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
        var token = new JwtSecurityToken(
            issuer: "Ryadom", audience: "RyadomAngular", claims: claims,
            notBefore: now.UtcDateTime, expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, account.UserName, account.Role);
    }
}

public static class PrincipalUser
{
    public static Guid AccountId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var id) || id == Guid.Empty)
            throw new UnauthorizedAccessException("Токен не содержит корректный идентификатор пользователя.");
        return id;
    }
}
