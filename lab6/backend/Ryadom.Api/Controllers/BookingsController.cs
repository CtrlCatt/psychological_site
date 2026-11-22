using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/bookings")]
public sealed class BookingsController(BookingService bookings) : ControllerBase
{
    [HttpGet]
    public PagedResult<Booking> List(
        [FromQuery] PageQuery pagination) =>
        bookings.List(User.AccountId(), pagination.ToRequest());

    [HttpGet("{id}")]
    public Booking Get(
        [FromRoute, ResourceId] string id) =>
        bookings.Get(User.AccountId(), Guid.Parse(id));

    [HttpPost]
    [RequestSizeLimit(4096)]
    public ActionResult<Booking> Create([FromBody] BookingInput input)
    {
        var created = bookings.Create(User.AccountId(), input.SpecialistId, input.SlotId);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(4096)]
    public Booking Update([FromRoute, ResourceId] string id, [FromBody] RescheduleInput input) =>
        bookings.Update(User.AccountId(), Guid.Parse(id), input.SlotId);

    [HttpDelete("{id}")]
    public IActionResult Delete(
        [FromRoute, ResourceId] string id)
    {
        bookings.Delete(User.AccountId(), Guid.Parse(id));
        return NoContent();
    }
}
