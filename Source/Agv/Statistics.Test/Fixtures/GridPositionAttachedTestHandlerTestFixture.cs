using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Statistics.Cache;
using Gebhardt.Acc.Statistics.Test.Models;
using Gebhardt.Acc.Statistics.Test.Services.EventHandler;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class GridPositionAttachedTestHandlerTestFixture
{
    private readonly Mock<IReadOnlyAgvScopedPositionCache<Guid>> _positionCacheMock = new();

    public Mock<IAgvScopedPositionCache<TestMessage>> PendingCacheMock { get; } = new();

    public Mock<IPublishEndpoint> PublishEndpointMock { get; } = new();

    public TestHandler Handler { get; }

    public GridPositionAttachedTestHandlerTestFixture()
    {
        Handler = new TestHandler(_positionCacheMock.Object, PendingCacheMock.Object, PublishEndpointMock.Object, Mock.Of<ILogger<TestHandler>>());
    }

    public static ConsumeContext<TestMessage> CreateContext(TestMessage message)
        => Mock.Of<ConsumeContext<TestMessage>>(x => x.Message == message);

    public GridPositionAttachedTestHandlerTestFixture WithCachedPosition(string serial, AgvPosition pos, List<Guid> gridIds)
    {
        _positionCacheMock.Setup(x => x.TryGet(serial, pos, out gridIds!)).Returns(true);
        return this;
    }

    public GridPositionAttachedTestHandlerTestFixture WithNoCachedPosition()
    {
        _positionCacheMock.Setup(x => x.TryGet(It.IsAny<string>(), It.IsAny<AgvPosition>(), out It.Ref<List<Guid>?>.IsAny)).Returns(false);
        return this;
    }

    public GridPositionAttachedTestHandlerTestFixture WithPendingMessages(string serial, AgvPosition pos, List<TestMessage> messages)
    {
        PendingCacheMock.Setup(x => x.TryRemove(serial, pos, out messages!)).Returns(true);
        return this;
    }

    public GridPositionAttachedTestHandlerTestFixture WithNoPendingMessages()
    {
        PendingCacheMock.Setup(x => x.TryRemove(It.IsAny<string>(), It.IsAny<AgvPosition>(), out It.Ref<List<TestMessage>?>.IsAny)).Returns(false);
        return this;
    }
}