using System.Net;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.WebApi;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.WebApi.Middleware;

/// <inheritdoc/>
/// <ínheritdoc/>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger)
{
    /// <inheritdoc/>
    protected override HttpStatusCode Convert(Exception exception)
    {
        var statusCode = exception switch
        {
            NotInCorrectStateException => HttpStatusCode.BadRequest,
            BadRequestException => HttpStatusCode.BadRequest,
            NotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };
        return statusCode;
    }
}