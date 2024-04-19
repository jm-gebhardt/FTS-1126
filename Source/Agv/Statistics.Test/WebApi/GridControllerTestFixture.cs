using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.Statistics.Messages.Commands;
using Gebhardt.StoreWare.Agv.Statistics.Messages.Requests;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Profiles;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.WebApi;

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