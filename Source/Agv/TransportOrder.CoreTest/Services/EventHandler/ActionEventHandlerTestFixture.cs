using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class ActionEventHandlerTestFixture : MassTransitConsumerTestFixture<ActionEventHandler>
{
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistry = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => _actionGroupRegistry.Object);
    }

    public ActionEventHandlerTestFixture()
    {
        Build();
    }

    public static IMapper GetMapper()
    {
        return MappingHelper.GetMapper(new ActionGroupProfile());
    }

    public void AddAssignedAgv(Guid? correlationId)
    {
        _actionGroupRegistry.Setup(x => x.GetAgvAssignedToAction(It.IsAny<Guid>())).ReturnsAsync(correlationId);
    }
}