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
    private Mock<IRequestClient<AddGrid>> _addGridClient = null!;
    private GotPositionFrequencies _gotPositionFrequencies = null!;
    private GotGrids _gotGrids = null!;

    protected override IEnumerable<Profile> GetProfiles() => [new GridProfile(), new PositionFrequencyProfile()];

    protected override void SetupController()
    {
        AddRequestClient<GetPositionFrequencies, GotPositionFrequencies>(() => _gotPositionFrequencies);
        AddRequestClient<GetGrids, GotGrids>(() => _gotGrids);
        _addGridClient = AddRequestClient<AddGrid, Success>(new Success());
    }

    public new GridControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public GridControllerTestFixture WithGrids(List<Grid> grids)
    {
        _gotGrids = new GotGrids(grids);
        return this;
    }

    public GridControllerTestFixture WithPositionFrequency(PositionFrequency positionFrequency)
    {
        _gotPositionFrequencies = new GotPositionFrequencies(positionFrequency);
        return this;
    }

    public void VerifyGridAdded()
    {
        _addGridClient.Verify(x => x.GetResponse<Success>(It.IsAny<AddGrid>(), default, default), Times.Once);
    }
}