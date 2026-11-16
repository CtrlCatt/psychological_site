using System.Data;
using Npgsql;
using NpgsqlTypes;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Infrastructure;

public sealed class SqlBookingRepository(NpgsqlDataSource dataSource) : IBookingRepository
{
    private const string SelectBooking = """
        SELECT b.id, b.slot_id, slot.specialist_id, specialist.full_name, slot.starts_at
        FROM bookings AS b
        JOIN consultation_slots AS slot ON slot.id = b.slot_id
        JOIN specialists AS specialist ON specialist.id = slot.specialist_id
        """;

    private const string ListSql = SelectBooking + """

        WHERE b.visitor_id = @visitor_id
        ORDER BY slot.starts_at, b.id
        LIMIT @limit OFFSET @offset;
        """;

    private const string FindSql = SelectBooking + """

        WHERE b.visitor_id = @visitor_id AND b.id = @id;
        """;

    private const string CountSql = "SELECT count(*) FROM bookings WHERE visitor_id = @visitor_id;";

    private const string InsertSql = """
        INSERT INTO bookings (id, visitor_id, slot_id) VALUES (@id, @visitor_id, @slot_id);
        """;

    private const string UpdateSql = """
        UPDATE bookings SET slot_id = @slot_id
        WHERE visitor_id = @visitor_id AND id = @id AND slot_id = @previous_slot_id;
        """;

    private const string DeleteSql = "DELETE FROM bookings WHERE visitor_id = @visitor_id AND id = @id;";

    public PagedResult<Booking> List(Guid visitorId, PageRequest page)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using var count = new NpgsqlCommand(CountSql, connection, transaction);
        count.Parameters.AddWithValue("visitor_id", NpgsqlDbType.Uuid, visitorId);
        var total = checked((int)(long)count.ExecuteScalar()!);

        using var list = new NpgsqlCommand(ListSql, connection, transaction);
        list.Parameters.AddWithValue("visitor_id", NpgsqlDbType.Uuid, visitorId);
        list.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, page.PageSize);
        list.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, page.Offset);
        var items = new List<Booking>();
        using (var reader = list.ExecuteReader())
        {
            while (reader.Read()) items.Add(ReadBooking(reader));
        }
        transaction.Commit();
        return new(items, page.Page, page.PageSize, total);
    }

    public Booking? Find(Guid visitorId, Guid id)
    {
        using var command = Command(FindSql, visitorId, id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadBooking(reader) : null;
    }

    public void Add(Guid visitorId, Booking booking)
    {
        using var command = Command(InsertSql, visitorId, booking.Id);
        command.Parameters.AddWithValue("slot_id", NpgsqlDbType.Varchar, booking.SlotId);
        WriteBooking(command);
    }

    public bool Update(Guid visitorId, Booking previous, Booking replacement)
    {
        using var command = Command(UpdateSql, visitorId, previous.Id);
        command.Parameters.AddWithValue("previous_slot_id", NpgsqlDbType.Varchar, previous.SlotId);
        command.Parameters.AddWithValue("slot_id", NpgsqlDbType.Varchar, replacement.SlotId);
        return WriteBooking(command) == 1;
    }

    public bool Delete(Guid visitorId, Guid id)
    {
        using var command = Command(DeleteSql, visitorId, id);
        return command.ExecuteNonQuery() == 1;
    }

    private NpgsqlCommand Command(string sql, Guid visitorId, Guid id)
    {
        var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("visitor_id", NpgsqlDbType.Uuid, visitorId);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        return command;
    }

    private static Booking ReadBooking(NpgsqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetFieldValue<DateTimeOffset>(4));

    private static int WriteBooking(NpgsqlCommand command)
    {
        try
        {
            return command.ExecuteNonQuery();
        }
        catch (PostgresException error) when (
            error.SqlState == PostgresErrorCodes.UniqueViolation && error.ConstraintName == "uq_bookings_slot")
        {
            throw new Conflict("Это время уже занято. Выберите другой слот.");
        }
    }
}
