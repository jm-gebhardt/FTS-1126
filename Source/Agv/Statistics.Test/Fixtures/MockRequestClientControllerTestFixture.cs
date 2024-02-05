using AutoMapper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.Fixtures;

/// <summary>
/// Provides a controller test fixture with mock request clients.
/// </summary>
/// <typeparam name="TController"></typeparam>
public class MockRequestClientControllerTestFixture<TController>
    where TController : Controller
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();
    private ServiceProvider _provider = null!;

    [UsedImplicitly]
    protected MockRequestClientControllerTestFixture<TController> Build()
    {
        var collection = new ServiceCollection();
        foreach (Action<IServiceCollection> service in _serviceActions)
        {
            service.Invoke(collection);
        }
        _provider = collection.BuildServiceProvider(true);
        Controller = ActivatorUtilities.CreateInstance<TController>(_provider);
        return this;
    }

    /// <summary>
    /// Gets the controller.
    /// </summary>
    public TController Controller { get; private set; } = null!;

    [UsedImplicitly]
    protected MockRequestClientControllerTestFixture<TController> AddRequestClientMock<TMessage>(Mock<IRequestClient<TMessage>> clientMock)
        where TMessage : class
    {
        _serviceActions.Add(s => s.AddTransient<IRequestClient<TMessage>>(_ => clientMock.Object));
        return this;
    }

    [UsedImplicitly]
    protected MockRequestClientControllerTestFixture<TController> AddMapper(IMapper mapper)
    {
        _serviceActions.Add(s => s.AddTransient<IMapper>(_ => mapper));
        return this;
    }
}