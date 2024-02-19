using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System.Net;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Middleware;

[TestClass]
public class ExceptionHandlerMiddlewareTest
{
    [TestMethod]
    public async Task Invoke_RequestHandlerThrowsMapNotFoundException_ReturnsNotFoundStatusCode()
    {
        var defaultContext = new DefaultHttpContext();

        var exceptionMiddleware = new ExceptionHandlerMiddleware(Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        await exceptionMiddleware.InvokeAsync(defaultContext, _ => throw new MapNotFoundException());

        defaultContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
        defaultContext.Response.ContentType.ShouldBe("application/json");
        defaultContext.Response.Body.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Invoke_RequestHandlerThrowsNodeNotFoundException_ReturnsBadRequestStatusCode()
    {
        var defaultContext = new DefaultHttpContext();

        var exceptionMiddleware = new ExceptionHandlerMiddleware(Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        await exceptionMiddleware.InvokeAsync(defaultContext, _ => throw new NodeNotFoundException("a node that was not found"));

        defaultContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        defaultContext.Response.ContentType.ShouldBe("application/json");
        defaultContext.Response.Body.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Invoke_RequestHandlerThrowsInvalidNodeTypeException_ReturnsBadRequestStatusCode()
    {
        var defaultContext = new DefaultHttpContext();

        var exceptionMiddleware = new ExceptionHandlerMiddleware(Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        await exceptionMiddleware.InvokeAsync(defaultContext, _ => throw new InvalidNodeTypeException("a node", "an invalid type", "an expected type"));

        defaultContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        defaultContext.Response.ContentType.ShouldBe("application/json");
        defaultContext.Response.Body.ShouldNotBeNull();
    }
}