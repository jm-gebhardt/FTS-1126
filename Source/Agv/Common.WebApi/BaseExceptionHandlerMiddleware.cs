using System.Net;
using System.Text.Json;
using Gebhardt.Acc.Common.Domain.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.WebApi;

/// <summary>
/// Handler for exceptions that can occur during execution of controllers
/// </summary>
/// <inheritdoc cref="BaseExceptionHandlerMiddleware"/>
public abstract class BaseExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : IMiddleware
{
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — no response needed, suppress the error log
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Convert <see cref="Exception"/> to <see cref="HttpStatusCode"/>.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The status code to be used in the response.</returns>
    protected virtual HttpStatusCode Convert(Exception exception)
    {
        var statusCode = exception switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            NotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };
        return statusCode;
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Exception occurred during request processing");

        if (exception is RequestFaultException or RequestException && exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        var problem = new ProblemDetails
        {
            Detail = exception.Message,
            Type = exception.GetType().ToString()
        };

        var statusCode = Convert(exception);

        string result = JsonSerializer.Serialize(problem);
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(result);
    }
}