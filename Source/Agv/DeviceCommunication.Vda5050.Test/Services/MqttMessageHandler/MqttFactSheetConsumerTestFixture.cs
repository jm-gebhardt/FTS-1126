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
using Gebhardt.Acc.Common.Domain.Utilities;

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

    /// <summary>
    /// Configures the agv repository to return an already known agv for the incoming fact sheet's serial number.
    /// </summary>
    public MqttFactSheetConsumerTestFixture WithExistingAgv(bool publishedToFleetManager = true, string manufacturer = "", string seriesName = "series", string serialNumber = "agv01")
    {
        var agv = new Agv(serialNumber.ToGuid(), manufacturer, seriesName, serialNumber) { PublishedToFleetManager = publishedToFleetManager };
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    /// <summary>
    /// Configures the agv type repository to return an already known agv type. By default, the type is registered
    /// under its own identifier (derived from <paramref name="manufacturer"/>/<paramref name="seriesName"/>); pass
    /// <paramref name="lookupIdentifier"/> to register it under a different identifier, e.g. to simulate the
    /// previously assigned type of an agv whose type is about to change.
    /// </summary>
    public MqttFactSheetConsumerTestFixture WithExistingAgvType(string manufacturer = "", string seriesName = "series", double length = 1.0, double width = 1.0, bool acknowledged = true, string? lookupIdentifier = null)
    {
        string identifier = $"{manufacturer}.{seriesName}";
        var agvType = new AgvType(identifier, manufacturer, seriesName, length, width, "raw-data", acknowledged);
        _agvTypeRepository.Setup(x => x.GetAgvTypeAsync(lookupIdentifier ?? identifier)).ReturnsAsync(agvType);
        return this;
    }

    public void VerifyAgvTypeAdded(Times times)
    {
        _agvTypeRepository.Verify(x => x.AddAsync(It.IsAny<AgvType>()), times);
    }
}
