using System.Net;
using System.Text.Json;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Middleware;

/// <summary>
/// Handler for exceptions that can occur during execution of controllers
/// </summary>
public class ExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger _logger;

    /// <inheritdoc cref="ExceptionHandlerMiddleware"/>
    public ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Exception occurred during request processing");
        HttpStatusCode statusCode;

        if (exception is RequestFaultException or RequestException && exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        var problem = new ProblemDetails
        {
            Detail = exception.Message,
            Type = exception.GetType().ToString()
        };

        switch (exception)
        {
            case NotInCorrectStateException:
            case ActionGroupStateInvalidException:
            case InvalidNodeTypeException:
            case ActionGroupInvalidException:
            case NodeNotFoundException:
            case NoNodesAvailableException:
            case BadRequestException:
                statusCode = HttpStatusCode.BadRequest;
                break;

            case BatchQueryException:
                statusCode = HttpStatusCode.BadRequest;
                break;

            case ResetAgvErrorFailedException:
                statusCode = HttpStatusCode.InternalServerError;
                break;
            
            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                break;
        }

        string result = JsonSerializer.Serialize(problem);
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(result);
    }
}