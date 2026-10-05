using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

public abstract class ControllerTestFixture<TRequest, TRequestHandler, TController> : MediatorTestFixture<TRequest, TRequestHandler, MockConsumer>
    where TRequest : class
    where TRequestHandler : class, IConsumer
    where TController : Controller
{
    private IRequestClient<TRequest> GetRequestClient()
    {
        return Mediator!.CreateRequestClient<TRequest>();
    }

    private readonly List<Claim> _claims = [];

    public TController Controller { get; private set; } = null!;

    protected abstract TController GetController();

    protected IRequestClient<TClient> GetRequestClient<TClient>() where TClient : class
    {
        return typeof(TRequest) == typeof(TClient) ? (IRequestClient<TClient>)GetRequestClient() : null!;
    }

    protected HttpContext? HttpContextOverride { get; set; }

    public override MediatorTestFixture<TRequest, TRequestHandler, MockConsumer> Build()
    {
        base.Build();
        var httpContext = HttpContextOverride ?? new DefaultHttpContext();

        if (_claims.Count == 0)
        {
            _claims.Add(new Claim(ClaimTypes.Email, "test@example.com"));
        }

        var identity = new ClaimsIdentity(_claims, "TestAuthentication");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        httpContext.User = claimsPrincipal;
        Controller = GetController();
        Controller.ControllerContext = new ControllerContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor()));

        return this;
    }

    public T WithClaim<T>(Claim claim) where T : ControllerTestFixture<TRequest, TRequestHandler, TController>
    {
        _claims.Add(claim);
        return (T)this;
    }

    public async Task GetControllerResultAsync<TResponse>(Func<TController, Task<ActionResult>> controllerAction) where TResponse : class
    {
        var controllerResult = await controllerAction(Controller);
        var result = controllerResult as TResponse;
        result.ShouldNotBeNull();
    }

    public async Task<TResponse> GetControllerResultAsync<TResult, TResponse>(Func<TController, Task<ActionResult<TResult>>> controllerAction) where TResponse : class
    {
        var controllerResult = await controllerAction(Controller);
        var result = controllerResult.Result as TResponse;
        result.ShouldNotBeNull();

        return result;
    }

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

    public async Task<TResult> GetControllerResultAsync<TResult>(Func<TController, Task<ActionResult<TResult>>> controllerAction) where TResult : class
    {
        var okResult = await GetControllerResultAsync<TResult, OkObjectResult>(controllerAction);
        var result = okResult.Value as TResult;
        result.ShouldNotBeNull();
        return result;
    }
}