using Ryadom.Domain;

namespace Ryadom.Application;

public readonly record struct PageRequest(int Page, int PageSize)
{
    public int Offset => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public bool HasNext => (long)Page * PageSize < Total;
}

public interface ICatalogRepository
{
    PagedResult<AssessmentSummary> ListAssessments(PageRequest page, bool includeExpress);
    Assessment? FindAssessment(string id);
    PagedResult<Specialist> ListSpecialists(PageRequest page);
    Specialist? FindSpecialist(string id);
    PagedResult<ConsultationSlot> ListSlots(string specialistId, DateTimeOffset now, PageRequest page);
    ConsultationSlot? FindSlot(string id);
}

public interface IResultRepository
{
    PagedResult<AssessmentResult> List(Guid visitorId, PageRequest page);
    AssessmentResult? Find(Guid visitorId, Guid id);
    void Add(Guid visitorId, AssessmentResult result);
    bool Delete(Guid visitorId, Guid id);
}

public interface IBookingRepository
{
    PagedResult<Booking> List(Guid visitorId, PageRequest page);
    Booking? Find(Guid visitorId, Guid id);
    void Add(Guid visitorId, Booking booking);
    bool Update(Guid visitorId, Booking previous, Booking replacement);
    bool Delete(Guid visitorId, Guid id);
}
