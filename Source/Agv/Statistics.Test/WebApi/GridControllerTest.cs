using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Statistics.Domain.Models;
using Statistics.Domain.ReadModels;
using Statistics.WebApi.Dtos;

namespace Statistics.Test.WebApi;

[TestClass]
public class GridControllerTest
{
    [TestMethod]
    public async Task GetGrids_ValidData_ReturnsGridDtos()
    {
        var grids = new List<Grid>() { new("a grid", 1, 1, 1, 1, 1, 1) };
        var fixture = new GridControllerTestFixture()
            .Setup()
            .WithGrids(grids);

        ActionResult<IEnumerable<GridDto>> response = await fixture.Controller.GetGrids();

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var gridDtos = result.Value as IList<GridDto>;
        gridDtos.ShouldNotBeNull();
        gridDtos.Count.ShouldBe(grids.Count);
    }

    [TestMethod]
    public async Task GetPositionFrequency_ValidData_ReturnsPositionFrequencyDtos()
    {
        var gridId = Guid.NewGuid();
        var positionFrequency = new PositionFrequency(gridId, new List<ICollection<float>>());

        var fixture = new GridControllerTestFixture()
            .Setup()
            .WithPositionFrequency(positionFrequency);

        ActionResult<PositionFrequencyDto> response = await fixture.Controller.GetPositionFrequency(gridId);

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var positionFrequencyDto = result.Value as PositionFrequencyDto;
        positionFrequencyDto.ShouldNotBeNull();
        positionFrequencyDto.GridId.ShouldBe(gridId);
    }
}