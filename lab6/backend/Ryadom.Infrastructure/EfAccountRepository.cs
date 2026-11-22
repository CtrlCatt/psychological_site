using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ryadom.Application;

namespace Ryadom.Infrastructure;

public sealed class EfAccountRepository(RyadomDbContext db) : IAccountRepository
{
    public Account? FindByNormalizedUserName(string normalized)
    {
        var row = db.Users.AsNoTracking().SingleOrDefault(x => x.NormalizedName == normalized);
        return row is null ? null : ToAccount(row);
    }

    public void Add(Account account)
    {
        db.Users.Add(new AccountRow
        {
            Id = account.Id,
            UserName = account.UserName,
            NormalizedName = account.UserName.ToUpperInvariant(),
            PasswordHash = account.PasswordHash,
            Role = account.Role,
            CreatedAt = account.CreatedAt
        });
        try { db.SaveChanges(); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException pg &&
            pg.SqlState == PostgresErrorCodes.UniqueViolation && pg.ConstraintName == "uq_app_users_normalized")
        {
            throw new Conflict("Этот логин уже занят.");
        }
    }

    public PagedResult<PublicAccount> List(PageRequest page)
    {
        var query = db.Users.AsNoTracking();
        var total = query.Count();
        var users = query.OrderBy(x => x.UserName).Skip(page.Offset).Take(page.PageSize)
            .Select(x => new PublicAccount(x.Id, x.UserName, x.Role, x.CreatedAt)).ToList();
        return new(users, page.Page, page.PageSize, total);
    }

    private static Account ToAccount(AccountRow row) =>
        new(row.Id, row.UserName, row.PasswordHash, row.Role, row.CreatedAt);
}
