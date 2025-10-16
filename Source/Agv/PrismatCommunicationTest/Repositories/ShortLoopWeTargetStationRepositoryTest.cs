using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Repositories;

[TestClass]
public class ShortLoopWeTargetStationRepositoryTest
{
    [TestMethod]
    public async Task GetTargetStation_ValidData_GetsShortLoopTarget()
    {
        var fixture = new ShortLoopWeTargetStationRepositoryTestFixture();

        fixture.Add(new ShortLoopWeTargetStation(Guid.NewGuid(), "Prim", "Prim"));

        var target = await fixture.Repository.GetTargetStation();
        target.LockedStations.ShouldBeEmpty();
        target.Current.ShouldBe("Prim");
        target.Primary.ShouldBe("Prim");
    }

    [TestMethod]
    public async Task UpdateStation_ValidData_UpdatesShortLoopTarget()
    {
        var fixture = new ShortLoopWeTargetStationRepositoryTestFixture();

        var station = new ShortLoopWeTargetStation(Guid.NewGuid(), "Prim", "Prim");

        fixture.Add(station);

        var update = new ShortLoopWeTargetStation(station.Id, station.Primary, "Sec");

        await fixture.Repository.UpdateAsync(update);
        
        var target = await fixture.Repository.GetTargetStation();
        target.LockedStations.ShouldBeEmpty();
        target.Primary.ShouldBe("Prim");
        target.Current.ShouldBe("Sec");
    }

    [TestMethod]
    public async Task LockStation_ValidData_StationLocked()
    {
        var fixture = new ShortLoopWeTargetStationRepositoryTestFixture();

        var station = new ShortLoopWeTargetStation(Guid.NewGuid(), "Prim", "Prim");

        fixture.Add(station);

        await fixture.Repository.LockStation(station.Primary);

        var target = await fixture.Repository.GetTargetStation();
        target.LockedStations.ShouldContain(station.Primary);
        target.Primary.ShouldBe("Prim");
        target.Current.ShouldBe("Prim");
    }

    [TestMethod]
    public async Task UnlockStation_ValidData_StationUnlocked()
    {
        var fixture = new ShortLoopWeTargetStationRepositoryTestFixture();

        var station = new ShortLoopWeTargetStation(Guid.NewGuid(), "Prim", "Prim") {LockedStations = { "Prim" }};

        fixture.Add(station);

        await fixture.Repository.UnlockStation(station.Primary);

        var target = await fixture.Repository.GetTargetStation();
        target.LockedStations.ShouldBeEmpty();
        target.Primary.ShouldBe("Prim");
        target.Current.ShouldBe("Prim");
    }
}