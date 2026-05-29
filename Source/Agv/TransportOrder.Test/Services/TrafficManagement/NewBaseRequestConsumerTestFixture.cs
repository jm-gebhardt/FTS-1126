using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain.Services.TrafficManagement;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.TrafficManagement;

public class NewBaseRequestConsumerTestFixture : MassTransitConsumerTestFixture<NewBaseRequestConsumer>
{
    public Mock<ITrafficManager> TrafficManager { get; } = new();

    public NewBaseRequestConsumerTestFixture()
    {
        AddServices(sc => sc.AddSingleton(_ => TrafficManager.Object));
        Build();
    }
}