using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Services.CommandHandler;
using Gebhardt.Acc.OrderTranslator.Test.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

public class StationCommandHandlerTestFixture: MassTransitConsumerTestFixture<StationCommandHandler>
{
    public readonly IStationRepository StationRepository = new StationRepositoryTestFixture().Repository;

    protected override FixtureBase AddServices(IServiceCollection collection)
    {   base.AddServices(collection);
        collection.AddTransient(_ => StationRepository);
        return this;
    }

    public StationCommandHandlerTestFixture()
    {
        Build();
    }
}