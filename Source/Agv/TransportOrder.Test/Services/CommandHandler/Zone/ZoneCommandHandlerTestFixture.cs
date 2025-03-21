using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Zone;

public class ZoneCommandHandlerTestFixture: MassTransitConsumerTestFixture<ZoneCommandHandler>
{
    private readonly Mock<IAgvDb> _agvDb = new();

    public ZoneCommandHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => MappingHelper.GetMapper(new AgvZoneProfile()));
        });
        Build();

        _agvDb.Setup(x => x.Zones.AddOrUpdateZone(It.IsAny<TransportOrder.Domain.Models.Zone.Zone>())).Verifiable();
    }

    public void VerifyZoneAdded()
    {
        _agvDb.Verify(x => x.Zones.AddOrUpdateZone(It.IsAny<TransportOrder.Domain.Models.Zone.Zone>()), Times.Once);
    }
}