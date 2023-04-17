using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Net;
using Gebhardt.StoreWare.Agv.Common.Exceptions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Middleware
{
    [TestClass]
    public class ExceptionHandlerMiddlewareTest
    {
        [TestMethod]
        public async Task Invoke_ShouldReturnNotFoundStatusCode_WhereRequestThrowsAMapNotFoundException()
        {
            var defaultContext = new DefaultHttpContext();

            var exceptionMiddleware = new ExceptionHandlerMiddleware();
            await exceptionMiddleware.InvokeAsync(defaultContext, _ => throw new MapNotFoundException());

            defaultContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
            defaultContext.Response.ContentType.ShouldBe("application/json");
            defaultContext.Response.Body.ShouldNotBeNull();
        }
    }
}
