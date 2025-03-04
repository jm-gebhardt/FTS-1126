using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class ActionCommandConsumerTestFixture : MqttConsumerTestFixture<ActionCommandConsumer>
{
    private readonly IGraphRepository _graphRepository = new GraphRepositoryTestFixture().WithGraphRepository().GraphRepository;
    private readonly Mock<IActionRepository> _actionRepository = new();

    public ActionCommandConsumerTestFixture()
    {
        _actionRepository.Setup(x => x.AddVehicleAndInternalOrderIdMapping(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        AddServices(s => s.AddTransient<IAgvRepository>(_ => Mock.Of<IAgvRepository>()));
        AddServices(s => s.AddTransient<IGraphRepository>(_ => _graphRepository));
        AddServices(s => s.AddTransient(_ => _actionRepository.Object));
        Build();
    }

    public void VerifyOrderMappingAdded(Times times)
    {
        _actionRepository.Verify(x => x.AddVehicleAndInternalOrderIdMapping(It.IsAny<Guid>(), It.IsAny<Guid>()), times);
    }
}