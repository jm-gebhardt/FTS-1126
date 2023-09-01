using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class ActionEventHandlerTestFixture : MassTransitConsumerTestFixture<ActionEventHandler>
{
    private readonly Mock<IActionManager> _actionManager = new();
    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _actionManager.Object);
    }

    public ActionEventHandlerTestFixture()
    {
        Build();
    }
}