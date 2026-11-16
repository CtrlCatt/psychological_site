using Microsoft.AspNetCore.Diagnostics;
using Ryadom.Application;
using Ryadom.Domain;

namespace Ryadom.Api;

public sealed class ApiErrors : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken cancellationToken)
    {
        var status = error switch
        {
            EntityNotFound => StatusCodes.Status404NotFound,
            Conflict => StatusCodes.Status409Conflict,
            RuleViolation => StatusCodes.Status422UnprocessableEntity,
            _ => 0
        };
        if (status == 0) return false;
        await Results.Problem(statusCode: status, detail: error.Message).ExecuteAsync(context);
        return true;
    }
}
