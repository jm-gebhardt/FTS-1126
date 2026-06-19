using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.RoutePlanHandler;

public class RoutePlanCommandHandlerTestFixture : MassTransitConsumerTestFixture<RoutePlanCommandHandler>
{
    public readonly AgvRoutePlanCache RoutePlanCache = new();

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [];
    }

    public RoutePlanCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<RoutePlanCommandHandler> AddServices(IServiceCollection collection)
    {
        collection.AddSingleton<IAgvRoutePlanCacheProvider>(_ => RoutePlanCache);
        return this;
    }
}