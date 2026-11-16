using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api.Controllers;

[ApiController]
[Route("api/assessments")]
public sealed class AssessmentsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public PagedResult<AssessmentSummary> List([FromQuery] PageQuery pagination, [FromQuery] bool includeExpress = false) =>
        catalog.ListAssessments(pagination.ToRequest(), includeExpress);

    [HttpGet("{id}")]
    public Assessment Get([FromRoute, StringLength(32), RegularExpression(Patterns.Slug)] string id) =>
        catalog.Assessment(id);
}
