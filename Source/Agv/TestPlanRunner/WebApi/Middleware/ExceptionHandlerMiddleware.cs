using System.Net;
using System.Text.Json;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.WebApi;

namespace Gebhardt.Acc.TestPlanRunner.WebApi.Middleware;

/// <summary>
/// Exception handler middleware.
/// </summary>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger)
{
    /// <inheritdoc />
    protected override HttpStatusCode Convert(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException or JsonException => HttpStatusCode.BadRequest,
            NotFoundException or System.IO.FileNotFoundException => HttpStatusCode.NotFound,
            IOException => HttpStatusCode.Conflict,
            _ => base.Convert(exception)
        };
    }
}
