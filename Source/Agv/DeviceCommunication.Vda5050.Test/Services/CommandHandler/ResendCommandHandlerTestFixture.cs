using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class ResendCommandHandlerTestFixture : MassTransitConsumerTestFixture<ResendCommandHandler>
{
    private readonly Mock<IAgvDataRepository> _agvDataRepository = new();

    public ResendCommandHandlerTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => _agvDataRepository.Object));
        Build();
    }

    public ResendCommandHandlerTestFixture AddAgvData(AgvData? agvData)
    {
        _agvDataRepository.Setup(x => x.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync(agvData);
        return this;
    }
}