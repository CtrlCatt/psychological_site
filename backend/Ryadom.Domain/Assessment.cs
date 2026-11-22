namespace Ryadom.Domain;

public sealed record Question(string Text, bool Sensitive = false);
public sealed record AssessmentSummary(string Id, string Title, string Label, string Description, string Time, int QuestionCount);

public sealed record Assessment(
    string Id,
    string Title,
    string Label,
    string Description,
    string Time,
    IReadOnlyList<Question> Questions,
    IReadOnlyList<string> Options)
{
    public AssessmentSummary Summarize() => new(Id, Title, Label, Description, Time, Questions.Count);

    public AssessmentResult Complete(Guid id, IReadOnlyList<int> answers, DateTimeOffset completedAt)
    {
        if (answers.Count != Questions.Count || answers.Any(value => value < 0 || value >= Options.Count))
            throw new RuleViolation("Укажите по одному допустимому ответу на каждый вопрос.");

        var score = answers.Sum();
        var maximum = Questions.Count * (Options.Count - 1);
        var needsSupport = Questions.Where((question, index) => question.Sensitive && answers[index] > 0).Any();
        return AssessmentResult.FromScore(id, Id, Title, score, maximum, needsSupport, completedAt);
    }
}

public sealed record AssessmentResult(
    Guid Id, string AssessmentId, string Title, int Score, int Maximum,
    string Level, string Note, bool NeedsSupport, DateTimeOffset CompletedAt)
{
    public static AssessmentResult FromScore(Guid id, string assessmentId, string title, int score,
        int maximum, bool needsSupport, DateTimeOffset completedAt)
    {
        var level = score * 3 >= maximum * 2 ? "high" : score * 3 >= maximum ? "medium" : "low";
        var note = level switch
        {
            "high" => "Высокий балл",
            "medium" => "Средний балл",
            _ => "Низкий балл"
        };
        return new AssessmentResult(id, assessmentId, title, score, maximum, level, note, needsSupport, completedAt);
    }
}
