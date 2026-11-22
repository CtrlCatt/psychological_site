namespace Ryadom.Infrastructure;

internal sealed class AssessmentRow
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required string Duration { get; init; }
    public int DisplayOrder { get; init; }
    public List<QuestionRow> Questions { get; } = [];
    public List<OptionRow> Options { get; } = [];
}

internal sealed class QuestionRow
{
    public required string AssessmentId { get; init; }
    public int Position { get; init; }
    public required string Text { get; init; }
    public bool Sensitive { get; init; }
}

internal sealed class OptionRow
{
    public required string AssessmentId { get; init; }
    public int Position { get; init; }
    public required string Text { get; init; }
}

internal sealed class SpecialistRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public int ExperienceYears { get; init; }
    public int PriceRub { get; init; }
    public decimal Rating { get; init; }
    public required string ImageUrl { get; init; }
    public int DisplayOrder { get; init; }
    public List<TopicRow> Topics { get; } = [];
}

internal sealed class TopicRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}

internal sealed class SlotRow
{
    public required string Id { get; init; }
    public required string SpecialistId { get; init; }
    public DateTimeOffset StartsAt { get; init; }
}

internal sealed class ResultRow
{
    public Guid Id { get; init; }
    public Guid VisitorId { get; init; }
    public required string AssessmentId { get; init; }
    public AssessmentRow Assessment { get; init; } = null!;
    public int Score { get; init; }
    public bool NeedsSupport { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}

internal sealed class BookingRow
{
    public Guid Id { get; init; }
    public Guid VisitorId { get; init; }
    public required string SlotId { get; init; }
}

internal sealed class AccountRow
{
    public Guid Id { get; init; }
    public required string UserName { get; init; }
    public required string NormalizedName { get; init; }
    public required string PasswordHash { get; init; }
    public required string Role { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
