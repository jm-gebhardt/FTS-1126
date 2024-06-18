using System.Net;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Middleware;

/// <inheritdoc/>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger)
{
    /// <inheritdoc/>
    protected override HttpStatusCode Convert(Exception exception)
    {
        var statusCode = exception switch
        {
            NotInCorrectStateException => HttpStatusCode.BadRequest,
            ActionGroupStateInvalidException => HttpStatusCode.BadRequest,
            ActionNotAllowedInModeException => HttpStatusCode.BadRequest,
            InvalidNodeTypeException => HttpStatusCode.BadRequest,
            InvalidAgvSettingsPropertyException => HttpStatusCode.BadRequest,
            ActionGroupInvalidException => HttpStatusCode.BadRequest,
            NodeNotFoundException => HttpStatusCode.BadRequest,
            BadRequestException => HttpStatusCode.BadRequest,
            BatchQueryException => HttpStatusCode.BadRequest,
            NodeNotAvailableException => HttpStatusCode.Conflict,
            NoNodesAvailableException => HttpStatusCode.Conflict,
            ResetAgvErrorFailedException => HttpStatusCode.InternalServerError,
            NotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };

        return statusCode;
    }
}