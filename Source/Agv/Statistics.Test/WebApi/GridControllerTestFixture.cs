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

    private ScalarHeatmap _heatmap = null!;
    private GotGrids _gotGrids = null!;

    protected override IEnumerable<Profile> GetProfiles() => [new GridProfile(), new HeatmapProfile()];

    protected override void SetupController()
    {
        AddRequestClient<GetStayDurations, GotStayDurations>(() => new GotStayDurations(_heatmap));
        AddRequestClient<GetErrorFrequencies, GotStayDurations>(() => new GotStayDurations(_heatmap));
        AddRequestClient<GetFieldViolationFrequencies, GotStayDurations>(() => new GotStayDurations(_heatmap));
        AddRequestClient<GetGrids, GotGrids>(() => _gotGrids);
        _addGridClient = AddRequestClient<AddGrid, Success>(new Success());
        AddRequestClient<GetWifiSignalStrength, GotWifiSignalStrength>(() => new GotWifiSignalStrength(_heatmap));
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

    public GridControllerTestFixture WithScalarHeatmap(ScalarHeatmap scalarHeatmap)
    {
        _heatmap = scalarHeatmap;
        return this;
    }

    public void VerifyGridAdded()
    {
        _addGridClient.Verify(x => x.GetResponse<Success>(It.IsAny<AddGrid>(), CancellationToken.None, default), Times.Once);
    }
}