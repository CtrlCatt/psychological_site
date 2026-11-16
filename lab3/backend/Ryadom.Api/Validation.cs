using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Ryadom.Application;

namespace Ryadom.Api;

public static class Patterns
{
    public const string Slug = @"\A[a-z0-9]+(?:-[a-z0-9]+)*\z";
}

public sealed class PageQuery
{
    [Range(1, 100_000)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 12;

    public PageRequest ToRequest() => new(Page, PageSize);
}

public sealed record ResultInput(
    [Required, StringLength(32), RegularExpression(Patterns.Slug)] string AssessmentId,
    [Required, MinLength(1), MaxLength(50)] int[] Answers);

public sealed record BookingInput(
    [Required, StringLength(32), RegularExpression(Patterns.Slug)] string SpecialistId,
    [Required, StringLength(64), RegularExpression(Patterns.Slug)] string SlotId);

public sealed record RescheduleInput(
    [Required, StringLength(64), RegularExpression(Patterns.Slug)] string SlotId);

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ResourceIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is string text && Guid.TryParseExact(text, "D", out var id) && id != Guid.Empty;
}

public sealed class ValidateRequestParameters : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalid = new Dictionary<string, string[]>();
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            var binding = parameter.BindingInfo;
            if (binding?.BindingSource == BindingSource.Header)
            {
                var header = binding.BinderModelName ?? parameter.Name;
                if (context.HttpContext.Request.Headers[header].Count != 1)
                    invalid[header] = ["Передайте ровно одно значение заголовка."];
            }
            if (binding?.BindingSource != BindingSource.Query) continue;
            if (parameter.ParameterType == typeof(PageQuery))
            {
                allowed.Add("page");
                allowed.Add("pageSize");
            }
            else
            {
                allowed.Add(binding.BinderModelName ?? parameter.Name);
            }
        }

        foreach (var entry in context.HttpContext.Request.Query)
        {
            if (!allowed.Contains(entry.Key) || entry.Value.Count != 1 || string.IsNullOrWhiteSpace(entry.Value[0]))
                invalid[entry.Key] = ["Недопустимый, пустой или повторяющийся параметр запроса."];
        }
        if (invalid.Count > 0)
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(invalid) { Status = 400 });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
