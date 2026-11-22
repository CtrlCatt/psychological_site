using Microsoft.EntityFrameworkCore;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Infrastructure;

public sealed class EfResultRepository(RyadomDbContext db) : IResultRepository
{
    public PagedResult<AssessmentResult> List(Guid visitorId, PageRequest page)
    {
        var query = db.Results.AsNoTracking().Where(x => x.VisitorId == visitorId);
        var items = Summaries(query.OrderByDescending(x => x.CompletedAt).ThenBy(x => x.Id)
                .Skip(page.Offset).Take(page.PageSize))
            .AsEnumerable().Select(ToResult).ToArray();
        return new(items, page.Page, page.PageSize, query.Count());
    }

    public AssessmentResult? Find(Guid visitorId, Guid id)
    {
        var row = Summaries(db.Results.AsNoTracking().Where(x => x.VisitorId == visitorId && x.Id == id))
            .SingleOrDefault();
        return row is null ? null : ToResult(row);
    }

    public void Add(Guid visitorId, AssessmentResult result)
    {
        db.Results.Add(new ResultRow
        {
            Id = result.Id,
            VisitorId = visitorId,
            AssessmentId = result.AssessmentId,
            Score = result.Score,
            NeedsSupport = result.NeedsSupport,
            CompletedAt = result.CompletedAt
        });
        db.SaveChanges();
    }

    public bool Delete(Guid visitorId, Guid id) =>
        db.Results.Where(x => x.VisitorId == visitorId && x.Id == id).ExecuteDelete() == 1;

    private static IQueryable<ResultSummary> Summaries(IQueryable<ResultRow> query) =>
        query.Select(x => new ResultSummary(x.Id, x.AssessmentId, x.Assessment.Title, x.Score,
            x.Assessment.Questions.Count * (x.Assessment.Options.Count - 1), x.NeedsSupport, x.CompletedAt));

    private static AssessmentResult ToResult(ResultSummary row) =>
        AssessmentResult.FromScore(row.Id, row.AssessmentId, row.Title, row.Score, row.Maximum, row.NeedsSupport, row.CompletedAt);

    private sealed record ResultSummary(Guid Id, string AssessmentId, string Title, int Score, int Maximum,
        bool NeedsSupport, DateTimeOffset CompletedAt);
}
