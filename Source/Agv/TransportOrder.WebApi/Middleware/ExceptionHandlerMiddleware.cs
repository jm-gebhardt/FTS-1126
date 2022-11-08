using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Middleware
{
    /// <summary>
    /// Handler for exceptions that can occur during execution of controllers
    /// </summary>
    public class ExceptionHandlerMiddleware : IMiddleware
    {
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
            HttpStatusCode statusCode;
            var problem = new ProblemDetails
            {
                Detail = exception.Message,
                Type = exception.GetType().ToString()
            };

            switch (exception)
            {
                case MapNotFoundException:
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
}
