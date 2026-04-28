using System.Net;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Filter.Exceptions;
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
            AgvNotAssignedToActionGroupException => HttpStatusCode.BadRequest,
            InvalidNodeTypeException => HttpStatusCode.BadRequest,
            InvalidAgvSettingsPropertyException => HttpStatusCode.BadRequest,
            ActionGroupInvalidException => HttpStatusCode.BadRequest,
            ValidationException => HttpStatusCode.BadRequest,
            BatchQueryException => HttpStatusCode.BadRequest,
            TriggerInstanceCountException => HttpStatusCode.BadRequest,
            TriggerAssignmentFailedException => HttpStatusCode.BadRequest,
            PositionIsInRestrictedAreaException => HttpStatusCode.BadRequest,
            InvalidPolygonLengthException => HttpStatusCode.BadRequest,
            TriggerConfigurationUpdateFailedException => HttpStatusCode.BadRequest,
            QueueNodeExistsException => HttpStatusCode.BadRequest,
            NodeNotAvailableException => HttpStatusCode.Conflict,
            NoNodesAvailableException => HttpStatusCode.Conflict,
            ResetAgvErrorFailedException => HttpStatusCode.InternalServerError,
            NotFoundException => HttpStatusCode.NotFound,
            AllAgvsOfflineException => HttpStatusCode.Conflict,
            AgvNotInZoneException => HttpStatusCode.Conflict,
            BitmapDecodingException => HttpStatusCode.BadRequest,
            FilterBuilderException => HttpStatusCode.BadRequest,
            FilterDtoParserException => HttpStatusCode.UnprocessableEntity,
            FilterParserException => HttpStatusCode.BadRequest,
            FilterExecutionException => HttpStatusCode.BadRequest,
            TriggerNotDeletedException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        return statusCode;
    }
}