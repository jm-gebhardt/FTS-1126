using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.WebApi.Controllers;
using Gebhardt.Acc.Statistics.WebApi.Profiles;
using MassTransit;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.WebApi;

public class GridControllerTestFixture : MockRequestClientControllerTestFixture<GridController>
{
    private Mock<IRequestClient<GetPositionFrequencies>> GetPositionFrequencyClient { get; } = new();

    private Mock<IRequestClient<GetGrids>> GetGridsClient { get; } = new();

    private Mock<IRequestClient<AddGrid>> AddGridClient { get; } = new();

    protected override IEnumerable<Profile> GetProfiles() => [new GridProfile(), new PositionFrequencyProfile()];

    public GridControllerTestFixture()
    {
        AddGridClient.Setup(x => x.GetResponse<Success>(It.IsAny<AddGrid>(), default, default)).Verifiable();
        AddRequestClientMock(GetPositionFrequencyClient);
        AddRequestClientMock(GetGridsClient);
        AddRequestClientMock(AddGridClient);
        Build();
    }

    public GridControllerTestFixture WithGrids(List<Grid> grids)
    {
        GetGridsClient.Setup(x => x.GetResponse<GotGrids>(It.IsAny<GetGrids>(), default, default)
            .Result.Message).Returns(new GotGrids(grids));
        return this;
    }

    public GridControllerTestFixture WithPositionFrequency(PositionFrequency positionFrequency)
    {
        GetPositionFrequencyClient.Setup(x => x.GetResponse<GotPositionFrequencies>(It.IsAny<GetPositionFrequencies>(), default, default)
            .Result.Message).Returns(new GotPositionFrequencies(positionFrequency));
        return this;
    }

    public void VerifyGridAdded()
    {
        AddGridClient.Verify(x => x.GetResponse<Success>(It.IsAny<AddGrid>(), default, default), Times.Once);
    }
}