using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

public class PublisherFixture<TPublisher, THub, TClient> where TPublisher : class where TClient : class where THub : Hub<TClient>
{
    private readonly ServiceCollection _serviceCollection = new();

    public Mapper Mapper { get; private set; } = null!;

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
    }

    public PublisherFixture<TPublisher, THub, TClient> WithMapper(params Profile[] profiles)
    {
        Mapper = MappingHelper.GetMapper(profiles);
        _serviceCollection.AddSingleton<IMapper>(Mapper);
        return this;
    }

    public PublisherFixture<TPublisher, THub, TClient> Build()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider(true);
        Publisher = serviceProvider.GetRequiredService<TPublisher>();
        return this;
    }
}