using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.Common.Testing.Filters;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

public class PublisherFixture<TPublisher, THub, TClient> : FixtureBase where TPublisher : class where TClient : class where THub : Hub<TClient>
{
    // ReSharper disable once ConvertToConstant.Global Otherwise we would have to write the full class name with type arguments everywhere, e.g.:
    // PublisherFixture<ActionGroupPublisher, ActionGroupHub, IActionGroupClient>.TestConnectionId
    public readonly string TestConnectionId = "test-connection-id";

    public Mock<IHubContext<THub, TClient>> HubContextMock { get; }

    public TPublisher Publisher { get; private set; } = null!;

    public PublisherFixture()
    {
        var mockHubClient = new Mock<TClient>();
        HubContextMock = new Mock<IHubContext<THub, TClient>>();
        var hubLifetimeManagerMock = new Mock<IAbortableHubLifetimeManager<THub>>();

        HubContextMock.Setup(h => h.Clients.Client(TestConnectionId)).Returns(mockHubClient.Object);
        hubLifetimeManagerMock.Setup(h => h.GetAllConnectionIds()).Returns(new List<string> { TestConnectionId });
        AddServices(s =>
        {
            s.AddSingleton<IHubContext<THub, TClient>>(_ => HubContextMock.Object);
            s.AddSingleton<IHubBroadcaster<THub, TClient>, HubBroadcaster<THub, TClient>>();
            s.AddSingleton<IAbortableHubLifetimeManager<THub>>(_ => hubLifetimeManagerMock.Object);
            s.AddSingleton<TPublisher>();
            s.AddSingleton<IMessageFilter<AgvPositionStatusDto>>(new FilterMock<AgvPositionStatusDto>(true));
            s.AddLogging();
        });
    }

    public new PublisherFixture<TPublisher, THub, TClient> Build()
    {
        base.Build();
        Publisher = Provider.GetRequiredService<TPublisher>();
        return this;
    }
}