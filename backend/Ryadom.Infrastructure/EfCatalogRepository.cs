using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Infrastructure;

public sealed class EfCatalogRepository(RyadomDbContext db) : ICatalogRepository
{
    public PagedResult<AssessmentSummary> ListAssessments(PageRequest page, bool includeExpress)
    {
        var query = db.Assessments.AsNoTracking().Where(x => includeExpress || x.Id != "express");
        var items = query.OrderBy(x => x.DisplayOrder).Skip(page.Offset).Take(page.PageSize)
            .Select(x => new AssessmentSummary(x.Id, x.Title, x.Label, x.Description, x.Duration, x.Questions.Count))
            .ToArray();
        return new(items, page.Page, page.PageSize, query.Count());
    }

    public Assessment? FindAssessment(string id)
    {
        var row = db.Assessments.AsNoTracking().Where(x => x.Id == id)
            .Include(x => x.Questions).Include(x => x.Options).AsSplitQuery().SingleOrDefault();
        return row is null ? null : new Assessment(row.Id, row.Title, row.Label, row.Description, row.Duration,
            row.Questions.OrderBy(x => x.Position).Select(x => new Question(x.Text, x.Sensitive)).ToArray(),
            row.Options.OrderBy(x => x.Position).Select(x => x.Text).ToArray());
    }

    public PagedResult<Specialist> ListSpecialists(PageRequest page)
    {
        var items = db.Specialists.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Skip(page.Offset).Take(page.PageSize).Include(x => x.Topics)
            .ToArray().Select(ToSpecialist).ToArray();
        return new(items, page.Page, page.PageSize, db.Specialists.Count());
    }

    public Specialist? FindSpecialist(string id)
    {
        var row = db.Specialists.AsNoTracking().Include(x => x.Topics).SingleOrDefault(x => x.Id == id);
        return row is null ? null : ToSpecialist(row);
    }

    public PagedResult<ConsultationSlot> ListSlots(string specialistId, DateTimeOffset now, PageRequest page)
    {
        var query = db.Slots.AsNoTracking().Where(slot =>
            slot.SpecialistId == specialistId && slot.StartsAt > now &&
            !db.Bookings.Any(booking => booking.SlotId == slot.Id));
        var items = query.OrderBy(x => x.StartsAt).ThenBy(x => x.Id).Skip(page.Offset).Take(page.PageSize)
            .Select(x => new ConsultationSlot(x.Id, x.SpecialistId, x.StartsAt)).ToArray();
        return new(items, page.Page, page.PageSize, query.Count());
    }

    public ConsultationSlot? FindSlot(string id) =>
        db.Slots.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new ConsultationSlot(x.Id, x.SpecialistId, x.StartsAt)).SingleOrDefault();

    private static Specialist ToSpecialist(SpecialistRow row)
    {
        var years = row.ExperienceYears;
        var lastDigit = years % 10;
        var suffix = years % 100 is >= 11 and <= 14 ? "лет" :
            lastDigit == 1 ? "год" : lastDigit is >= 2 and <= 4 ? "года" : "лет";
        var price = row.PriceRub.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
        return new(row.Id, row.Name, row.Role,
            string.Join(" · ", row.Topics.OrderBy(x => x.Id).Select(x => x.Name)),
            $"{years} {suffix} опыта", $"{price} ₽", row.Rating.ToString("0.0", CultureInfo.InvariantCulture), row.ImageUrl);
    }
}
