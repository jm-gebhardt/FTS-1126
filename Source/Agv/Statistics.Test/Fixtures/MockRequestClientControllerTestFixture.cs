using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
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
}