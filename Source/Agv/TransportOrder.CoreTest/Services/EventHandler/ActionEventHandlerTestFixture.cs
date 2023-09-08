using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Profiles;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class ActionEventHandlerTestFixture : MassTransitConsumerTestFixture<ActionEventHandler>
{
    private readonly Mock<IActionManager> _actionManager = new();
    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _actionManager.Object);
        collection.AddTransient(_ => GetMapper());
    }

    public ActionEventHandlerTestFixture()
    {
        Build();
    }

    public static IMapper GetMapper()
    {
        return MappingHelper.GetMapper(new ActionGroupProfile());
    }
}