using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class InstantActionsCommandHandlerTestFixture : MassTransitConsumerTestFixture<InstantActionsCommandHandler>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public InstantActionsCommandHandlerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvRepository.Object);
            sc.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
        });
        Build();
    }

    public InstantActionsCommandHandlerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }
}