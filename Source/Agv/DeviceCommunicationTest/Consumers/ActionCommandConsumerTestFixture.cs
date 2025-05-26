using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Common.Repositories.IAgvRepository;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class ActionCommandConsumerTestFixture : MqttConsumerTestFixture<ActionCommandConsumer>
{
    private readonly GraphRepositoryTestFixture _graphRepositoryTestFixture = new();
    private readonly Mock<IActionRepository> _actionRepository = new();

    public ActionCommandConsumerTestFixture()
    {
        var graphRepository = _graphRepositoryTestFixture.GraphRepository;
        _actionRepository.Setup(x => x.AddVehicleAndInternalOrderIdMapping(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        AddServices(s => s.AddTransient<IAgvRepository>(_ => Mock.Of<IAgvRepository>()));
        AddServices(s => s.AddTransient<IGraphRepository>(_ => graphRepository));
        AddServices(s => s.AddTransient(_ => _actionRepository.Object));
        Build();
    }

    public async Task FillGraphRepository()
    {
        await _graphRepositoryTestFixture.FillGraphRepository();
    }

    public ActionCommandConsumerTestFixture WithVehicleAndOrderIdMapping(Guid orderId)
    {
        _actionRepository.Setup(x => x.GetInternalOrderId(It.IsAny<Guid>())).ReturnsAsync(orderId);
        return this;
    }

    public void VerifyOrderMappingAdded(Times times)
    {
        _actionRepository.Verify(x => x.AddVehicleAndInternalOrderIdMapping(It.IsAny<Guid>(), It.IsAny<Guid>()), times);
    }

    public void VerifyOrderMappingRemoved(Times times)
    {
        _actionRepository.Verify(x => x.RemoveVehicleAndInternalOrderIdMapping(It.IsAny<Guid>()), times);
    }
}