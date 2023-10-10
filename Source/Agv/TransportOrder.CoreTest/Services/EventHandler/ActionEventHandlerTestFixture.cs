using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class ActionEventHandlerTestFixture : MassTransitConsumerTestFixture<ActionEventHandler>
{
    protected override void ConfigureServices(IServiceCollection collection)
    {
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