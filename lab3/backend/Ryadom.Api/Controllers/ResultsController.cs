using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/results")]
public sealed class ResultsController(ResultService results) : ControllerBase
{
    private const string VisitorHeader = "X-Visitor-Id";

    [HttpGet]
    public PagedResult<AssessmentResult> List(
        [FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromQuery] PageQuery pagination) =>
        results.List(Guid.Parse(visitor), pagination.ToRequest());

    [HttpGet("{id}")]
    public AssessmentResult Get(
        [FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromRoute, ResourceId] string id) =>
        results.Get(Guid.Parse(visitor), Guid.Parse(id));

    [HttpPost]
    [RequestSizeLimit(4096)]
    public ActionResult<AssessmentResult> Create([FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromBody] ResultInput input)
    {
        var created = results.Create(Guid.Parse(visitor), input.AssessmentId, input.Answers);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(
        [FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromRoute, ResourceId] string id)
    {
        results.Delete(Guid.Parse(visitor), Guid.Parse(id));
        return NoContent();
    }
}
