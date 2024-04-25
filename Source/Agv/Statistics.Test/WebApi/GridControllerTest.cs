using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.WebApi;

[TestClass]
public class GridControllerTest
{
    [TestMethod]
    public async Task GetGrids_ValidData_ReturnsGridDtos()
    {
        var grids = new List<Grid>() { new("a grid", 1, 1, 1, 1, 1, 1) };
        var fixture = new GridControllerTestFixture()
            .WithGrids(grids)
            .Build();

        List<GridDto> gridDtos = (await fixture.GetControllerResultValueAsync(c => c.GetGrids())).ToList();

        gridDtos.ShouldNotBeNull();
        gridDtos.Count.ShouldBe(grids.Count);
    }

    [TestMethod]
    public async Task AddGrid_ValidData_GridAdded()
    {
        var fixture = new GridControllerTestFixture()
            .Build();
        await fixture.GetControllerResultAsync<OkResult>(c => c.AddGrid("name", 1, 1));
        fixture.VerifyGridAdded();
    }

    [TestMethod]
    public async Task GetPositionFrequency_ValidData_ReturnsPositionFrequencyDtos()
    {
        var gridId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        DateTime? fromDate = DateTime.UtcNow;
        DateTime? toDate = DateTime.UtcNow;
        
        var positionFrequency = new CountHeatmap(gridId, mapId, new List<ICollection<float>>());

        var fixture = new GridControllerTestFixture()
            .WithCountHeatmap(positionFrequency)
            .Build();
        
        var positionFrequencyDto = await fixture.GetControllerResultValueAsync(c => c.GetPositionFrequency(gridId, mapId, fromDate, toDate));

        positionFrequencyDto.GridId.ShouldBe(gridId);
        positionFrequencyDto.MapId.ShouldBe(mapId);
    }
}