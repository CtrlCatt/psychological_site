namespace Ryadom.Domain;

public sealed record Specialist(
    string Id, string Name, string Role, string Themes, string Experience, string Price, string Rating, string Image);

public sealed record ConsultationSlot(string Id, string SpecialistId, DateTimeOffset StartsAt)
{
    public void CheckAvailableAt(DateTimeOffset now)
    {
        if (StartsAt <= now)
            throw new RuleViolation("Это время уже прошло. Выберите другое.");
    }
}

public sealed record Booking(Guid Id, string SlotId, string SpecialistId, string SpecialistName, DateTimeOffset StartsAt)
{
    public static Booking Create(Guid id, Specialist specialist, ConsultationSlot slot, DateTimeOffset now)
    {
        if (slot.SpecialistId != specialist.Id)
            throw new RuleViolation("Слот не принадлежит выбранному специалисту.");

        slot.CheckAvailableAt(now);
        return new Booking(id, slot.Id, specialist.Id, specialist.Name, slot.StartsAt);
    }

    public Booking Reschedule(ConsultationSlot slot, DateTimeOffset now)
    {
        if (slot.SpecialistId != SpecialistId)
            throw new RuleViolation("При переносе выберите время у того же специалиста.");

        slot.CheckAvailableAt(now);
        return this with { SlotId = slot.Id, StartsAt = slot.StartsAt };
    }
}
