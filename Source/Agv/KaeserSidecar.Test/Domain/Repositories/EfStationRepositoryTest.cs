using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class EfStationRepositoryTest
{
    [TestMethod]
    public async Task OccupyStation_FirstTimeOccupied_StationOccupancyStored()
    {
        var agv = Guid.NewGuid();
        var fixture = new StationRepositoryTestFixture();

        var station = "station".ToGuid();

        await fixture.Repository.OccupyStation(station, agv);

        ICollection<StationOccupancy> stationOccupancies = await fixture.Repository.GetStationOccupancy();

        var stationOccupancy = stationOccupancies.Single(s => s.StationId == station);
        stationOccupancy.StationId.ShouldBe(station);
        stationOccupancy.Count.ShouldBe(1);
        stationOccupancy.AgvCorrelationId.ShouldBe(agv);
    }

    [TestMethod]
    public async Task FreeStation_ValidData_OccupancyUpdated()
    {
        var agv = Guid.NewGuid();
        var fixture = new StationRepositoryTestFixture();
        var stationOccupancy = new StationOccupancy("station".ToGuid(), "station", 1, agv);
        await fixture.Repository.AddAsync(stationOccupancy);

        await fixture.Repository.FreeStation(stationOccupancy.StationId);

        (await fixture.Repository.GetStationOccupancy()).Single(o => o.StationId == stationOccupancy.StationId).Count.ShouldBe(stationOccupancy.Count - 1);
    }

    [TestMethod]
    public async Task GetStationOccupancy_NoPriorOccupancies_OccupanciesReturned()
    {
        var fixture = new StationRepositoryTestFixture();

        ICollection<StationOccupancy> occupancies = await fixture.Repository.GetStationOccupancy();

        occupancies.Count.ShouldBe(3);
        occupancies.Sum(o => o.Count).ShouldBe(0);
    }
}