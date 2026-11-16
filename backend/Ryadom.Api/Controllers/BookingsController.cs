using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/bookings")]
public sealed class BookingsController(BookingService bookings) : ControllerBase
{
    private const string VisitorHeader = "X-Visitor-Id";

    [HttpGet]
    public PagedResult<Booking> List(
        [FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromQuery] PageQuery pagination) =>
        bookings.List(Guid.Parse(visitor), pagination.ToRequest());

    [HttpGet("{id}")]
    public Booking Get(
        [FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromRoute, ResourceId] string id) =>
        bookings.Get(Guid.Parse(visitor), Guid.Parse(id));

    [HttpPost]
    [RequestSizeLimit(4096)]
    public ActionResult<Booking> Create([FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromBody] BookingInput input)
    {
        var created = bookings.Create(Guid.Parse(visitor), input.SpecialistId, input.SlotId);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(4096)]
    public Booking Update([FromHeader(Name = VisitorHeader), ResourceId] string visitor,
        [FromRoute, ResourceId] string id, [FromBody] RescheduleInput input) =>
        bookings.Update(Guid.Parse(visitor), Guid.Parse(id), input.SlotId);

    [HttpDelete("{id}")]
    public IActionResult Delete(
        [FromHeader(Name = VisitorHeader), ResourceId] string visitor, [FromRoute, ResourceId] string id)
    {
        bookings.Delete(Guid.Parse(visitor), Guid.Parse(id));
        return NoContent();
    }
}
