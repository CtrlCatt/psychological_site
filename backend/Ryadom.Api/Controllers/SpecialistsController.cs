using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api.Controllers;

[ApiController]
[Route("api/specialists")]
public sealed class SpecialistsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public PagedResult<Specialist> List([FromQuery] PageQuery pagination) => catalog.ListSpecialists(pagination.ToRequest());

    [HttpGet("{id}")]
    public Specialist Get([FromRoute, StringLength(32), RegularExpression(Patterns.Slug)] string id) =>
        catalog.Specialist(id);

    [HttpGet("{id}/slots")]
    public PagedResult<ConsultationSlot> ListSlots(
        [FromRoute, StringLength(32), RegularExpression(Patterns.Slug)] string id,
        [FromQuery] PageQuery pagination) => catalog.ListSlots(id, pagination.ToRequest());
}
