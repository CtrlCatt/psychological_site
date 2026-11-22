using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;

namespace Ryadom.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/admin/users")]
public sealed class AdminController(AuthenticationService accounts) : ControllerBase
{
    [HttpGet]
    public PagedResult<PublicAccount> List([FromQuery] PageQuery pagination) =>
        accounts.ListAccounts(pagination.ToRequest());
}
