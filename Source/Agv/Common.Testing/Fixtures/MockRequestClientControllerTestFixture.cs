using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Security.Claims;
using RouteData = Microsoft.AspNetCore.Routing.RouteData;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Provides a controller test fixture with mock request clients.
/// </summary>
/// <typeparam name="TController"></typeparam>
public abstract class MockRequestClientControllerTestFixture<TController> : FixtureBase
    where TController : Controller
{
    /// <summary>
    /// Gets the controller.
    /// </summary>
    private TController _controller = null!;

    protected abstract void SetupController();

    [UsedImplicitly]
    protected MockRequestClientControllerTestFixture<TController> AddRequestClientMock<TMessage>(Mock<IRequestClient<TMessage>> clientMock)
        where TMessage : class
    {
        AddServices(s =>
        {
            s.AddTransient(_ => new Mock<ILogger<TController>>().Object);
            s.AddTransient<IRequestClient<TMessage>>(_ => clientMock.Object);
        });
        return this;
    }

    [PublicAPI]
    protected Mock<IRequestClient<TRequest>> AddRequestClient<TRequest, TResponse>(Func<TResponse> response)
        where TRequest : class
        where TResponse : class
    {
        var requestClientMock = new Mock<IRequestClient<TRequest>>();
        AddRequestClientMock(requestClientMock);

        requestClientMock.Setup(x => x.GetResponse<TResponse>(
                    It.IsAny<TRequest>(), CancellationToken.None, default)
                .Result.Message)
            .Returns(response);

        return requestClientMock;
    }

    protected Mock<IRequestClient<TRequest>> AddRequestClient<TRequest, TResponse>(TResponse response)
        where TRequest : class
        where TResponse : class
        => AddRequestClient<TRequest, TResponse>(() => response);

    protected new void Build()
    {
        SetupController();
        base.Build();
        _controller = ActivatorUtilities.CreateInstance<TController>(Provider);
        var httpContext =new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        httpContext.User = claimsPrincipal;
        _controller.ControllerContext = new ControllerContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor()));
    }

    [PublicAPI]
    public async Task GetControllerResultAsync<TResponse>(Func<TController, Task<ActionResult>> controllerAction) where TResponse : class
    {
        var controllerResult = await controllerAction(_controller);
        var result = controllerResult as TResponse;
        result.ShouldNotBeNull();
    }

    [PublicAPI]
    public async Task<TResponse> GetControllerResultAsync<TResult, TResponse>(Func<TController, Task<ActionResult<TResult>>> controllerAction) where TResponse : class
    {
        var controllerResult = await controllerAction(_controller);
        var result = controllerResult.Result as TResponse;
        result.ShouldNotBeNull();

        return result;
    }

    [PublicAPI]
    public async Task<TResult> GetControllerResultValueAsync<TResult>(Func<TController, Task<ActionResult<TResult>>> controllerAction)
    {
        var okResult = await GetControllerResultAsync<TResult, OkObjectResult>(controllerAction);
        var result = okResult.Value is TResult value ? value : default;
        if (result == null)
        {
            throw new Exception("Result is null");
        }
        return result;
    }

    [PublicAPI]
    public async Task<TResult> GetControllerResultAsync<TResult>(Func<TController, Task<ActionResult<TResult>>> controllerAction) where TResult : class
    {
        var okResult = await GetControllerResultAsync<TResult, OkObjectResult>(controllerAction);
        var result = okResult.Value as TResult;
        result.ShouldNotBeNull();
        return result;
    }
}