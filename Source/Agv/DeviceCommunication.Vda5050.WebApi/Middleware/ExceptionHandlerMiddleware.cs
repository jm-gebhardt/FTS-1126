using System.Net;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.WebApi;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Middleware;

/// <inheritdoc/>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger)
{
    /// <inheritdoc/>
    protected override HttpStatusCode Convert(Exception exception)
    {
        var statusCode = exception switch
        {
            NotInCorrectStateException => HttpStatusCode.BadRequest,
            BadRequestException => HttpStatusCode.BadRequest,
            NodeNotAvailableException => HttpStatusCode.Conflict,
            NoNodesAvailableException => HttpStatusCode.Conflict,
            NotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };

        return statusCode;
    }
}