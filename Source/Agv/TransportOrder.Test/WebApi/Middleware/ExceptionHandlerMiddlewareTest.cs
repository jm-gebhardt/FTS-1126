using System.Net;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using Gebhardt.Acc.TransportOrder.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Middleware;

[TestClass]
public class ExceptionHandlerMiddlewareTest
{
    [DynamicData(nameof(GetExceptionTestData), DynamicDataSourceType.Method)]
    [TestMethod]
    public async Task Invoke_Exception_ReturnsExpectedStatusCode(Func<Exception> exceptionFunc, HttpStatusCode expectedStatusCode)
    {
        var defaultContext = new DefaultHttpContext();
        var exceptionMiddleware = new ExceptionHandlerMiddleware(Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        var exceptionInstance = exceptionFunc.Invoke();

        await exceptionMiddleware.InvokeAsync(defaultContext, _ => throw exceptionInstance);
        defaultContext.Response.StatusCode.ShouldBe((int) expectedStatusCode);
        defaultContext.Response.ContentType.ShouldBe("application/json");
        defaultContext.Response.Body.ShouldNotBeNull();
    }

    public static IEnumerable<object[]> GetExceptionTestData()
    {
        // BadRequest
        yield return [() => new NotInCorrectStateException(null!), HttpStatusCode.BadRequest];
        yield return [() => new ActionGroupStateInvalidException(Mock.Of<ActionGroup>()), HttpStatusCode.BadRequest];
        yield return [() => new InvalidNodeTypeException(null!, null!, null!), HttpStatusCode.BadRequest];
        yield return [() => new ActionGroupInvalidException(Array.Empty<string>()), HttpStatusCode.BadRequest];
        yield return [() => new NodeNotFoundException(null!), HttpStatusCode.BadRequest];
        yield return [() => new BadRequestException(null!), HttpStatusCode.BadRequest];
        yield return [() => new InvalidPolygonLengthException(), HttpStatusCode.BadRequest];
        yield return [() => new AgvNotAssignedToActionGroupException(string.Empty), HttpStatusCode.BadRequest];
        yield return [() => new AgvNotAssignedToActionGroupException(Guid.Empty, Guid.Empty), HttpStatusCode.BadRequest];
        yield return [() => new AgvsNotFoundException(new List<Guid>()), HttpStatusCode.BadRequest];
        yield return [() => new ActionNotAllowedInModeException(Guid.Empty, null!), HttpStatusCode.BadRequest];
        yield return [() => new PositionIsInRestrictedAreaException(0, 0), HttpStatusCode.BadRequest];

        // Conflict
        yield return [() => new ChargeStationNotAvailableException(null!), HttpStatusCode.Conflict];
        yield return [() => new ParkNodeNotAvailableException(""), HttpStatusCode.Conflict];
        yield return [() => new NoChargeStationAvailableException(), HttpStatusCode.Conflict];
        yield return [() => new NodeNotAvailableException(null!), HttpStatusCode.Conflict];
        yield return [() => new NoNodesAvailableException(null!), HttpStatusCode.Conflict];

        // InternalServerError
        yield return [() => new ResetAgvErrorFailedException(null!, null!), HttpStatusCode.InternalServerError];
        yield return [() => new Exception(), HttpStatusCode.InternalServerError];

        // NotFound
        yield return [() => new ActionGroupNotFoundException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new ActionNotFoundException(null!, null!, null!), HttpStatusCode.NotFound];
        yield return [() => new AgvDataNotFoundException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new AgvGraphNotDefinedException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new AgvNotFoundException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new AgvStatusNotFoundException(Mock.Of<TransportOrder.Domain.Models.Agv.Agv>()), HttpStatusCode.NotFound];
        yield return [() => new AgvNotFoundException(string.Empty), HttpStatusCode.NotFound];
        yield return [() => new AgvStatusReceivedNotFoundException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new MapNotFoundException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new OrderNotFoundException(Guid.Empty), HttpStatusCode.NotFound];
        yield return [() => new PropertyNotFoundException(null!), HttpStatusCode.NotFound];
        yield return [() => new RouteNotFoundException("", ""), HttpStatusCode.NotFound];
    }
}