using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.Common.Testing.Filters;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Hubs;

public class PublisherFixture<TPublisher, THub, TClient> : FixtureBase where TPublisher : class where TClient : class where THub : Hub<TClient>
{
    public Mock<IHubContext<THub, TClient>> HubContextMock { get; }

    public TPublisher Publisher { get; private set; } = null!;

    public PublisherFixture()
    {
        var mockHubClient = new Mock<TClient>();
        var mockClients = new Mock<IHubClients<TClient>>();
        HubContextMock = new Mock<IHubContext<THub, TClient>>();

        mockClients.SetupGet(h => h.All).Returns(mockHubClient.Object);
        HubContextMock.SetupGet(h => h.Clients).Returns(mockClients.Object);
        AddServices(s =>
        {
            s.AddSingleton<IHubContext<THub, TClient>>(_ => HubContextMock.Object);
            s.AddSingleton<TPublisher>();
            s.AddSingleton<IMessageFilter<AgvBatteryStatusDto>>(new FilterMock<AgvBatteryStatusDto>(true));
            s.AddSingleton<IMessageFilter<AgvPositionStatusDto>>(new FilterMock<AgvPositionStatusDto>(true));
        });
    }

    public new PublisherFixture<TPublisher, THub, TClient> Build()
    {
        base.Build();
        Publisher = Provider.GetRequiredService<TPublisher>();
        return this;
    }
}