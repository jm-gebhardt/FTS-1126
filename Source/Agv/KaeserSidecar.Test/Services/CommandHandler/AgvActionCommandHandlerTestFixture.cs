using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

public class AgvActionCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvActionCommandHandler>
{
    public readonly IStationRepository Repository;
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();

    protected override MassTransitConsumerTestFixture<AgvActionCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<AgvActionCommandHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton<IStationRepository, StationRepository>();
        collection.AddTransient(_ => _transportOrderDb.Object);
        return this;
    }

    public AgvActionCommandHandlerTestFixture()
    {
        Build();
        Repository = Provider.GetRequiredService<IStationRepository>();
    }

    public AgvActionCommandHandlerTestFixture WithActionGroup(ActionGroup? actionGroup)
    {
        _transportOrderDb.Setup(x => x.ActionGroups.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid _) =>
            {
                if (actionGroup != null)
                {
                    return actionGroup;
                }
                throw new Exception();
            });
        return this;
    }
}