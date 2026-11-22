using Ryadom.Domain;

namespace Ryadom.Application;

public sealed record Account(Guid Id, string UserName, string PasswordHash, string Role, DateTimeOffset CreatedAt);
public sealed record PublicAccount(Guid Id, string UserName, string Role, DateTimeOffset CreatedAt);
public sealed record AuthSession(string AccessToken, DateTimeOffset ExpiresAt, string UserName, string Role);

public interface IAccountRepository
{
    Account? FindByNormalizedUserName(string normalized);
    void Add(Account account);
    PagedResult<PublicAccount> List(PageRequest page);
}

public interface IPasswordService
{
    string Hash(Account account, string password);
    bool Verify(Account account, string password);
}

public interface IAccessTokenIssuer
{
    AuthSession Issue(Account account);
}

public sealed class InvalidCredentials : Exception { }

public sealed class AuthenticationService(
    IAccountRepository accounts, IPasswordService passwordService, IAccessTokenIssuer tokens,
    TimeProvider time)
{
    public AuthSession Register(string username, string password)
    {
        Validate(username, password);
        var normalized = Normalize(username);
        if (accounts.FindByNormalizedUserName(normalized) is not null)
            throw new Conflict("Этот логин уже занят.");

        var account = new Account(Guid.NewGuid(), username, "", "User", time.GetUtcNow());
        account = account with { PasswordHash = passwordService.Hash(account, password) };
        accounts.Add(account);
        return tokens.Issue(account);
    }

    public AuthSession Login(string username, string password)
    {
        // Не раскрываем, существует ли такой пользователь.
        var account = accounts.FindByNormalizedUserName(Normalize(username));
        if (account is null || !passwordService.Verify(account, password)) throw new InvalidCredentials();
        return tokens.Issue(account);
    }

    // Только для первоначальной настройки в Development; публичного API нет.
    public void EnsureDevelopmentAdmin(string username, string password)
    {
        Validate(username, password);
        if (accounts.FindByNormalizedUserName(Normalize(username)) is not null) return;
        var account = new Account(Guid.NewGuid(), username, "", "Admin", time.GetUtcNow());
        account = account with { PasswordHash = passwordService.Hash(account, password) };
        accounts.Add(account);
    }

    public PagedResult<PublicAccount> ListAccounts(PageRequest page) => accounts.List(page);

    private static string Normalize(string username) => username.Trim().ToUpperInvariant();

    private static void Validate(string username, string password)
    {
        if (username.Length is < 3 or > 32 ||
            !System.Text.RegularExpressions.Regex.IsMatch(username, @"\A[A-Za-z][A-Za-z0-9_]*\z"))
            throw new RuleViolation("Логин должен содержать 3–32 латинские буквы, цифры или знак _. Начните с буквы.");
        if (password.Length is < 10 or > 128 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new RuleViolation("Пароль должен содержать 10–128 символов, включая буквы и цифры.");
    }
}
