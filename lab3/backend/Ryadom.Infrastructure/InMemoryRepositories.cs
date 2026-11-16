using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Infrastructure;

public sealed class InMemoryCatalog(TimeProvider time, InMemoryBookings bookings) : ICatalogRepository
{
    private readonly Dictionary<string, ConsultationSlot> slots = CreateSlots(time.GetUtcNow());

    public PagedResult<AssessmentSummary> ListAssessments(PageRequest page, bool includeExpress) =>
        Paging.Slice(DemoCatalog.Assessments
            .Where(item => includeExpress || item.Id != "express")
            .Select(item => item.Summarize()), page);

    public Assessment? FindAssessment(string id) => DemoCatalog.Assessments.FirstOrDefault(item => item.Id == id);

    public PagedResult<Specialist> ListSpecialists(PageRequest page) => Paging.Slice(DemoCatalog.Specialists, page);
    public Specialist? FindSpecialist(string id) => DemoCatalog.Specialists.FirstOrDefault(item => item.Id == id);

    public PagedResult<ConsultationSlot> ListSlots(string specialistId, DateTimeOffset now, PageRequest page)
    {
        var booked = bookings.OccupiedSlots();
        return Paging.Slice(slots.Values
            .Where(slot => slot.SpecialistId == specialistId && slot.StartsAt > now && !booked.Contains(slot.Id))
            .OrderBy(slot => slot.StartsAt), page);
    }

    public ConsultationSlot? FindSlot(string id) => slots.GetValueOrDefault(id);

    private static Dictionary<string, ConsultationSlot> CreateSlots(DateTimeOffset now)
    {
        var start = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        return DemoCatalog.Specialists.SelectMany(specialist => Enumerable.Range(1, 7)
            .SelectMany(day => new[] { 9, 13, 16 }.Select(hour => start.AddDays(day).AddHours(hour)))
            .Select(date => new ConsultationSlot($"{specialist.Id}-{date:yyyyMMddHH}", specialist.Id, date)))
            .ToDictionary(slot => slot.Id);
    }
}

public sealed class InMemoryResults : IResultRepository
{
    private readonly object gate = new();
    private readonly Dictionary<(Guid VisitorId, Guid Id), AssessmentResult> entries = [];

    public PagedResult<AssessmentResult> List(Guid visitorId, PageRequest page)
    {
        lock (gate)
            return Paging.Slice(entries.Where(entry => entry.Key.VisitorId == visitorId)
                .Select(entry => entry.Value).OrderByDescending(result => result.CompletedAt).ThenBy(result => result.Id), page);
    }

    public AssessmentResult? Find(Guid visitorId, Guid id)
    {
        lock (gate) return entries.GetValueOrDefault((visitorId, id));
    }

    public void Add(Guid visitorId, AssessmentResult result)
    {
        lock (gate) entries.Add((visitorId, result.Id), result);
    }

    public bool Delete(Guid visitorId, Guid id)
    {
        lock (gate) return entries.Remove((visitorId, id));
    }
}

public sealed class InMemoryBookings : IBookingRepository
{
    private readonly object gate = new();
    private readonly Dictionary<(Guid VisitorId, Guid Id), Booking> entries = [];

    public PagedResult<Booking> List(Guid visitorId, PageRequest page)
    {
        lock (gate)
            return Paging.Slice(entries.Where(entry => entry.Key.VisitorId == visitorId)
                .Select(entry => entry.Value).OrderBy(booking => booking.StartsAt).ThenBy(booking => booking.Id), page);
    }

    public Booking? Find(Guid visitorId, Guid id)
    {
        lock (gate) return entries.GetValueOrDefault((visitorId, id));
    }

    public void Add(Guid visitorId, Booking booking)
    {
        lock (gate)
        {
            EnsureFree(booking.SlotId, booking.Id);
            entries.Add((visitorId, booking.Id), booking);
        }
    }

    public bool Update(Guid visitorId, Booking previous, Booking replacement)
    {
        lock (gate)
        {
            var key = (visitorId, previous.Id);
            if (entries.GetValueOrDefault(key) != previous) return false;
            EnsureFree(replacement.SlotId, previous.Id);
            entries[key] = replacement;
            return true;
        }
    }

    public bool Delete(Guid visitorId, Guid id)
    {
        lock (gate) return entries.Remove((visitorId, id));
    }

    internal HashSet<string> OccupiedSlots()
    {
        lock (gate) return entries.Values.Select(booking => booking.SlotId).ToHashSet();
    }

    private void EnsureFree(string slotId, Guid bookingId)
    {
        if (entries.Values.Any(booking => booking.SlotId == slotId && booking.Id != bookingId))
            throw new Conflict("Это время уже занято. Выберите другой слот.");
    }
}

internal static class Paging
{
    public static PagedResult<T> Slice<T>(IEnumerable<T> items, PageRequest page)
    {
        var snapshot = items.ToArray();
        return new PagedResult<T>(snapshot.Skip(page.Offset).Take(page.PageSize).ToArray(), page.Page, page.PageSize, snapshot.Length);
    }
}
