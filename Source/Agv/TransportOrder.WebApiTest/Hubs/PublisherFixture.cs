using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

public class PublisherFixture<TPublisher, THub, TClient> where TPublisher : class where TClient : class where THub : Hub<TClient>
{
    private readonly ServiceCollection _serviceCollection = new();

    public Mock<IMapper> MapperMock { get; } = new();

    public Mock<IHubContext<THub, TClient>> HubContextMock { get; }

    public TPublisher Publisher { get; private set; } = null!;

    public PublisherFixture()
    {
        var mockHubClient = new Mock<TClient>();
        var mockClients = new Mock<IHubClients<TClient>>();
        HubContextMock = new Mock<IHubContext<THub, TClient>>();

        mockClients.SetupGet(h => h.All).Returns(mockHubClient.Object);
        HubContextMock.SetupGet(h => h.Clients).Returns(mockClients.Object);
        _serviceCollection.AddSingleton<IHubContext<THub, TClient>>(_ => HubContextMock.Object);
        _serviceCollection.AddSingleton<TPublisher>();
        _serviceCollection.AddSingleton<IMapper>(_ => MapperMock.Object);
        _serviceCollection.AddSingleton<IHubFilter<AgvBatteryStatusDto>>(new FilterMock<AgvBatteryStatusDto>(true));
        _serviceCollection.AddSingleton<IHubFilter<AgvPositionStatusDto>>(new FilterMock<AgvPositionStatusDto>(true));
    }

    public PublisherFixture<TPublisher, THub, TClient> Build()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider(true);
        Publisher = serviceProvider.GetRequiredService<TPublisher>();
        return this;
    }
}