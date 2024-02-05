using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.Statistics.Messages.Requests;
using Gebhardt.StoreWare.Agv.Statistics.Test.Fixtures;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Profiles;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.WebApi;

public class GridControllerTestFixture : MockRequestClientControllerTestFixture<GridController>
{
    private Mock<IRequestClient<GetPositionFrequenciesForGrid>> GetPositionFrequencyClient { get; } = new();

    private Mock<IRequestClient<GetGrids>> GetGridsClient { get; } = new();

    public GridControllerTestFixture Setup()
    {
        AddMapper(MappingHelper.GetMapper(new GridProfile(),
            new PositionFrequencyProfile()));
        AddRequestClientMock(GetPositionFrequencyClient);
        AddRequestClientMock(GetGridsClient);
        Build();
        return this;
    }

    public GridControllerTestFixture WithGrids(List<Grid> grids)
    {
        GetGridsClient.Setup(x => x.GetResponse<GotGrids>(It.IsAny<GetGrids>(), default, default)
            .Result.Message).Returns(new GotGrids(grids));
        return this;
    }

    public GridControllerTestFixture WithPositionFrequency(PositionFrequency positionFrequency)
    {
        GetPositionFrequencyClient.Setup(x => x.GetResponse<GotPositionFrequenciesForGrid>(It.IsAny<GetPositionFrequenciesForGrid>(), default, default)
            .Result.Message).Returns(new GotPositionFrequenciesForGrid(positionFrequency));
        return this;
    }
}