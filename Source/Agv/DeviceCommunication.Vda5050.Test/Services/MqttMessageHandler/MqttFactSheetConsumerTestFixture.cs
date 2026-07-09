using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

public class MqttFactSheetConsumerTestFixture : MqttConsumerTestFixture<MqttFactSheetConsumer>
{
    private readonly Mock<IAgvDataRepository> _agvDataRepository = new();
    private readonly Mock<IAgvActionRepository> _agvActionRepository = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IAgvTypeRepository> _agvTypeRepository = new();

    public MqttFactSheetConsumerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvActionRepository.Object);
            sc.AddTransient(_ => _agvDataRepository.Object);
            sc.AddTransient(_ => _agvRepository.Object);
            sc.AddTransient(_ => _agvTypeRepository.Object);
        });
        _agvActionRepository.Setup(x => x.AddOrUpdateActionsAsync(It.IsAny<ICollection<AgvAction>>(), It.IsAny<string>())).Verifiable();
        _agvDataRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<AgvData>())).Verifiable();
        _agvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>())).Verifiable();
        _agvTypeRepository.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _agvTypeRepository.Setup(x => x.AddAsync(It.IsAny<AgvType>())).Returns(Task.CompletedTask);
        Build();
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper([
            typeof(Vda5050.Messages.Profiles.AgvTypeProfile).Assembly,
            typeof(Vda5050.Domain.Profiles.FactSheetProfile).Assembly
        ]);
    }

    public void VerifyDataUpdated()
    {
        _agvDataRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<AgvData>()), Times.Once);
    }

    public void VerifyActionsUpdated(Times times)
    {
        _agvActionRepository.Verify(x => x.AddOrUpdateActionsAsync(It.IsAny<ICollection<AgvAction>>(), It.IsAny<string>()), times);
    }

    public void VerifyAgvUpdated(Times times)
    {
        _agvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>()), times);
    }

    public MqttFactSheetConsumerTestFixture WithExistingAgvType()
    {
        _agvTypeRepository.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        return this;
    }

    public void VerifyAgvTypeAdded(Times times)
    {
        _agvTypeRepository.Verify(x => x.AddAsync(It.IsAny<AgvType>()), times);
    }
}