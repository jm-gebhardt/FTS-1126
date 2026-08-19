using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

public class FactSheetRequestHandlerTestFixture : MediatorTestFixture<AddOrUpdateFactSheet, FactSheetRequestHandler, MockConsumer>
{
    private readonly Mock<IAgvActionRepository> _agvActionRepository = new();

    protected override IEnumerable<Profile> GetProfiles() => [new FactSheetProfile(), new AgvActionProfile()];

    public FactSheetRequestHandlerTestFixture()
    {
        AddServices(s => s.AddTransient(_ => _agvActionRepository.Object));
        _agvActionRepository
            .Setup(x => x.AddOrUpdateActionsAsync(It.IsAny<ICollection<AgvAction>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        base.Build();
    }

    public FactSheetRequestHandlerTestFixture WithAddOrUpdateActionsThrows()
    {
        _agvActionRepository
            .Setup(x => x.AddOrUpdateActionsAsync(It.IsAny<ICollection<AgvAction>>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("storage failure"));
        return this;
    }

    public void VerifyActionsStored(string serialNumber, Times times)
    {
        _agvActionRepository.Verify(
            x => x.AddOrUpdateActionsAsync(It.IsAny<ICollection<AgvAction>>(), serialNumber),
            times);
    }
}
