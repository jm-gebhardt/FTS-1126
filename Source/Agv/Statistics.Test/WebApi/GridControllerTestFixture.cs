using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using MassTransit;
using Moq;
using Statistics.Domain.Models;
using Statistics.Domain.ReadModels;
using Statistics.Messages.Requests;
using Statistics.Test.Fixtures;
using Statistics.WebApi.Controllers;
using Statistics.WebApi.Profiles;

namespace Statistics.Test.WebApi;

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