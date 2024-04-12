using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.WebApi;

[TestClass]
public class GridControllerTest
{
    [TestMethod]
    public async Task GetGrids_ValidData_ReturnsGridDtos()
    {
        var grids = new List<Grid>() { new("a grid", 1, 1, 1, 1, 1, 1) };
        var fixture = new GridControllerTestFixture()
            .WithGrids(grids);

        ActionResult<IEnumerable<GridDto>> response = await fixture.Controller.GetGrids();

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var gridDtos = result.Value as IList<GridDto>;
        gridDtos.ShouldNotBeNull();
        gridDtos.Count.ShouldBe(grids.Count);
    }

    [TestMethod]
    public async Task AddGrid_ValidData_GridAdded()
    {
        var fixture = new GridControllerTestFixture();
        var response = await fixture.Controller.AddGrid("name", 1, 1);
        var result = response as OkResult;
        result.ShouldNotBeNull();
        fixture.VerifyGridAdded();
    }

    [TestMethod]
    public async Task GetPositionFrequency_ValidData_ReturnsPositionFrequencyDtos()
    {
        var gridId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        DateTime? fromDate = DateTime.UtcNow;
        DateTime? toDate = DateTime.UtcNow;
        
        var positionFrequency = new PositionFrequency(gridId, mapId, new List<ICollection<float>>());

        var fixture = new GridControllerTestFixture()
            .WithPositionFrequency(positionFrequency);

        ActionResult<PositionFrequencyDto> response = await fixture.Controller.GetPositionFrequency(gridId, mapId, fromDate, toDate);

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var positionFrequencyDto = result.Value as PositionFrequencyDto;
        positionFrequencyDto.ShouldNotBeNull();
        positionFrequencyDto.GridId.ShouldBe(gridId);
        positionFrequencyDto.MapId.ShouldBe(mapId);
    }
}