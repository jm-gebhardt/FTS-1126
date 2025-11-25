using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Test.Models;
using Gebhardt.Acc.Statistics.Test.Services.EventHandler;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class GridPositionAttachedTestHandlerTestFixture
{
    private readonly Mock<IStatisticsDb> _statisticsDbMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    public TestHandler Handler { get; }

    public GridPositionAttachedTestHandlerTestFixture()
    {
        Handler = new TestHandler(_statisticsDbMock.Object, _publishEndpointMock.Object, Mock.Of<ILogger<TestHandler>>());
    }

    public static ConsumeContext<TestMessage> CreateContext(TestMessage message)
        => Mock.Of<ConsumeContext<TestMessage>>(x => x.Message == message);

    public GridPositionAttachedTestHandlerTestFixture WithGridCells(List<GridCell> grids)
    {
        _statisticsDbMock.Setup(x => x.GridCell.GetOrAddGridCellsForCoordinatesAsync(It.IsAny<decimal>(), It.IsAny<decimal>())).ReturnsAsync(grids);
        return this;
    }
}