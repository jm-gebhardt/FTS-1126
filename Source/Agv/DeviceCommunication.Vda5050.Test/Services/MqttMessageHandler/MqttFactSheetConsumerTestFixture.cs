using AutoMapper.Internal;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
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

    public MqttFactSheetConsumerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvActionRepository.Object);
            sc.AddTransient(_ => _agvDataRepository.Object);
            sc.AddTransient(_ => _agvRepository.Object);
        });
        _agvActionRepository.Setup(x => x.AddOrUpdateActionsAsync(It.IsAny<ICollection<AgvAction>>(), It.IsAny<string>())).Verifiable();
        _agvDataRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<AgvData>())).Verifiable();
        _agvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>())).Verifiable();
        Build();
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.AddAutoMapper(cfg => cfg.Internal().ForAllMaps((_, map) => map.MaxDepth(10)), AppDomain.CurrentDomain.GetAssemblies());
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
}