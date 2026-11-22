using Ryadom.Domain;

namespace Ryadom.Application;

public sealed class EntityNotFound(string message) : Exception(message);
public sealed class Conflict(string message) : Exception(message);

public sealed class CatalogService(ICatalogRepository catalog, TimeProvider time)
{
    public PagedResult<AssessmentSummary> ListAssessments(PageRequest page, bool includeExpress) =>
        catalog.ListAssessments(page, includeExpress);

    public Assessment Assessment(string id) =>
        catalog.FindAssessment(id) ?? throw new EntityNotFound("Опросник не найден.");

    public PagedResult<Specialist> ListSpecialists(PageRequest page) => catalog.ListSpecialists(page);

    public Specialist Specialist(string id) =>
        catalog.FindSpecialist(id) ?? throw new EntityNotFound("Специалист не найден.");

    public PagedResult<ConsultationSlot> ListSlots(string specialistId, PageRequest page)
    {
        Specialist(specialistId);
        return catalog.ListSlots(specialistId, time.GetUtcNow(), page);
    }
}

public sealed class ResultService(CatalogService catalog, IResultRepository results, TimeProvider time)
{
    public PagedResult<AssessmentResult> List(Guid visitorId, PageRequest page) => results.List(visitorId, page);

    public AssessmentResult Get(Guid visitorId, Guid id) =>
        results.Find(visitorId, id) ?? throw new EntityNotFound("Результат не найден.");

    public AssessmentResult Create(Guid visitorId, string assessmentId, IReadOnlyList<int> answers)
    {
        var result = catalog.Assessment(assessmentId).Complete(Guid.NewGuid(), answers, time.GetUtcNow());
        results.Add(visitorId, result);
        return result;
    }

    public void Delete(Guid visitorId, Guid id)
    {
        if (!results.Delete(visitorId, id))
            throw new EntityNotFound("Результат не найден.");
    }
}

public sealed class BookingService(CatalogService catalog, ICatalogRepository slots, IBookingRepository bookings, TimeProvider time)
{
    public PagedResult<Booking> List(Guid visitorId, PageRequest page) => bookings.List(visitorId, page);

    public Booking Get(Guid visitorId, Guid id) =>
        bookings.Find(visitorId, id) ?? throw new EntityNotFound("Запись не найдена.");

    public Booking Create(Guid visitorId, string specialistId, string slotId)
    {
        var booking = Booking.Create(Guid.NewGuid(), catalog.Specialist(specialistId), FindSlot(slotId), time.GetUtcNow());
        bookings.Add(visitorId, booking);
        return booking;
    }

    public Booking Update(Guid visitorId, Guid id, string slotId)
    {
        var previous = Get(visitorId, id);
        var replacement = previous.Reschedule(FindSlot(slotId), time.GetUtcNow());
        if (!bookings.Update(visitorId, previous, replacement))
            throw new Conflict("Запись изменилась. Обновите список.");
        return replacement;
    }

    public void Delete(Guid visitorId, Guid id)
    {
        if (!bookings.Delete(visitorId, id))
            throw new EntityNotFound("Запись не найдена.");
    }

    private ConsultationSlot FindSlot(string id) =>
        slots.FindSlot(id) ?? throw new EntityNotFound("Время консультации не найдено.");
}
