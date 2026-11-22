using Ryadom.Domain;

namespace Ryadom.Tests;

public sealed class DomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Assessment Assessment = new("demo", "Тест", "Тема", "Описание", "1 минута",
        [new("Первый вопрос", true), new("Второй вопрос")], ["0", "1", "2", "3"]);
    private static readonly Specialist Specialist = new("anna", "Анна", "Психолог", "", "", "", "", "");

    [Theory]
    [InlineData(0, 1, 1, "low", false)]
    [InlineData(1, 1, 2, "medium", true)]
    [InlineData(3, 1, 4, "high", true)]
    public void AssessmentCalculatesDemoScoreAndSupportFlag(int first, int second, int score, string level, bool needsSupport)
    {
        var result = Assessment.Complete(Guid.NewGuid(), [first, second], Now);

        Assert.Equal(score, result.Score);
        Assert.Equal(6, result.Maximum);
        Assert.Equal(level, result.Level);
        Assert.Equal(needsSupport, result.NeedsSupport);
        Assert.Equal(Now, result.CompletedAt);
    }

    [Fact]
    public void AssessmentRequiresAnAllowedAnswerForEveryQuestion()
    {
        Assert.Throws<RuleViolation>(() => Assessment.Complete(Guid.NewGuid(), [2], Now));
        Assert.Throws<RuleViolation>(() => Assessment.Complete(Guid.NewGuid(), [0, 4], Now));
        Assert.Throws<RuleViolation>(() => Assessment.Complete(Guid.NewGuid(), [-1, 1], Now));
    }

    [Fact]
    public void BookingRejectsPastSlotsAndSlotsBelongingToAnotherSpecialist()
    {
        var valid = new ConsultationSlot("valid", Specialist.Id, Now.AddDays(1));
        var past = new ConsultationSlot("past", Specialist.Id, Now);
        var foreign = new ConsultationSlot("foreign", "elena", Now.AddDays(1));

        var booking = Booking.Create(Guid.NewGuid(), Specialist, valid, Now);

        Assert.Throws<RuleViolation>(() => Booking.Create(Guid.NewGuid(), Specialist, past, Now));
        Assert.Throws<RuleViolation>(() => Booking.Create(Guid.NewGuid(), Specialist, foreign, Now));
        Assert.Throws<RuleViolation>(() => booking.Reschedule(past, Now));
        Assert.Throws<RuleViolation>(() => booking.Reschedule(foreign, Now));
        var moved = booking.Reschedule(new ConsultationSlot("later", Specialist.Id, Now.AddDays(2)), Now);
        Assert.Equal(booking.Id, moved.Id);
        Assert.Equal("later", moved.SlotId);
    }
}
