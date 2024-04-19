using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;

/// <summary>
/// Provides a controller test fixture with mock request clients.
/// </summary>
/// <typeparam name="TController"></typeparam>
public class MockRequestClientControllerTestFixture<TController> : FixtureBase
    where TController : Controller
{
    /// <summary>
    /// Gets the controller.
    /// </summary>
    public TController Controller { get; private set; } = null!;

    [UsedImplicitly]
    protected MockRequestClientControllerTestFixture<TController> AddRequestClientMock<TMessage>(Mock<IRequestClient<TMessage>> clientMock)
        where TMessage : class
    {
        AddServices(s => s.AddTransient<IRequestClient<TMessage>>(_ => clientMock.Object));
        return this;
    }

    protected new void Build()
    {
        base.Build();
        Controller = ActivatorUtilities.CreateInstance<TController>(Provider);
    }

    [PublicAPI]
    public async Task GetControllerResultAsync<TResponse>(Func<TController, Task<ActionResult>> controllerAction) where TResponse : class
    {
        var controllerResult = await controllerAction(Controller);
        var result = controllerResult as TResponse;
        result.ShouldNotBeNull();
    }

    [PublicAPI]
    public async Task<TResponse> GetControllerResultAsync<TResult, TResponse>(Func<TController, Task<ActionResult<TResult>>> controllerAction) where TResponse : class
    {
        ActionResult<TResult> controllerResult = await controllerAction(Controller);
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