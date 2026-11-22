using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/results")]
public sealed class ResultsController(ResultService results) : ControllerBase
{
    [HttpGet]
    public PagedResult<AssessmentResult> List(
        [FromQuery] PageQuery pagination) =>
        results.List(User.AccountId(), pagination.ToRequest());

    [HttpGet("{id}")]
    public AssessmentResult Get(
        [FromRoute, ResourceId] string id) =>
        results.Get(User.AccountId(), Guid.Parse(id));

    [HttpPost]
    [RequestSizeLimit(4096)]
    public ActionResult<AssessmentResult> Create([FromBody] ResultInput input)
    {
        var created = results.Create(User.AccountId(), input.AssessmentId, input.Answers);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(
        [FromRoute, ResourceId] string id)
    {
        results.Delete(User.AccountId(), Guid.Parse(id));
        return NoContent();
    }
}
