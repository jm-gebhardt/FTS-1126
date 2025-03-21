using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class EfStationRepositoryTest
{
    [TestMethod]
    public async Task OccupyStation_FirstTimeOccupied_StationOccupancyStored()
    {
        var fixture = new StationRepositoryTestFixture();

        const string station = "station";

        await fixture.Repository.OccupyStation(station, Guid.NewGuid());

        ICollection<StationOccupancy> stationOccupancies = await fixture.Repository.GetStationOccupancy();

        var stationOccupancy = stationOccupancies.Single(s => s.Station == station);
        stationOccupancy.Station.ShouldBe(station);
        stationOccupancy.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task FreeStation_ValidData_OccupancyUpdated()
    {
        var fixture = new StationRepositoryTestFixture();
        var stationOccupancy = new StationOccupancy("station", 1);
        await fixture.Repository.AddAsync(stationOccupancy);

        await fixture.Repository.FreeStation(stationOccupancy.Station, Guid.NewGuid());

        (await fixture.Repository.GetStationOccupancy()).Single(o => o.Station == stationOccupancy.Station).Count.ShouldBe(stationOccupancy.Count - 1);
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