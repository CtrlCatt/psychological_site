using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ryadom.Application;

namespace Ryadom.Api.Controllers;

public sealed record CredentialsInput(
    [Required, RegularExpression(@"\A[A-Za-z][A-Za-z0-9_]{2,31}\z")] string UserName,
    [Required, StringLength(128, MinimumLength = 10)] string Password);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(AuthenticationService auth) : ControllerBase
{
    [HttpPost("register")]
    [RequestSizeLimit(4096)]
    public ActionResult<AuthSession> Register([FromBody] CredentialsInput request) =>
        StatusCode(StatusCodes.Status201Created, auth.Register(request.UserName, request.Password));

    [HttpPost("login")]
    [RequestSizeLimit(4096)]
    public AuthSession Login([FromBody] CredentialsInput request) => auth.Login(request.UserName, request.Password);
}
