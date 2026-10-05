using Shouldly;
using static Gebhardt.Acc.StationCommunication.Test.Services.PlcConnectionRepositoryTestFixture;

namespace Gebhardt.Acc.StationCommunication.Test.Services;

[TestClass]
public class PlcConnectionRepositoryTest
{
    [TestMethod]
    public void UpdateChannelState_AllChannelsConnect_StationBecomesOnline()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .WithStation(Ps02, FromWe, AplWe)
            .Build();

        var result1 = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        result1.ShouldBeEmpty();

        var result2 = fixture.Repository.UpdateChannelState(AplWe, true).ToList();
        result2.ShouldContain((Ps01Guid, true));
        result2.ShouldContain((Ps02Guid, true));
    }

    [TestMethod]
    public void UpdateChannelState_OneChannelDrops_StationBecomesOffline()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .WithStation(Ps02, FromWe, AplWe)
            .Build();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, true).ToList();

        var result = fixture.Repository.UpdateChannelState(FromWe, false).ToList();

        result.ShouldContain((Ps01Guid, false));
        result.ShouldContain((Ps02Guid, false));
    }

    [TestMethod]
    public void UpdateChannelState_SecondChannelDrops_NoAdditionalEvent()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .Build();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(FromWe, false).ToList();

        var result = fixture.Repository.UpdateChannelState(AplWe, false).ToList();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void UpdateChannelState_OneChannelReturns_StationRemainsOffline()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .Build();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(FromWe, false).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, false).ToList();

        var result = fixture.Repository.UpdateChannelState(FromWe, true).ToList();

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void UpdateChannelState_AllChannelsReturn_StationBecomesOnline()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .WithStation(Ps02, FromWe, AplWe)
            .Build();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, true).ToList();
        _ = fixture.Repository.UpdateChannelState(FromWe, false).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, false).ToList();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        var result = fixture.Repository.UpdateChannelState(AplWe, true).ToList();

        result.ShouldContain((Ps01Guid, true));
        result.ShouldContain((Ps02Guid, true));
    }

    [TestMethod]
    public void UpdateChannelState_StationWithNoPlcConnections_NeverEmitted()
    {
        var unmonitoredId = UnmonitoredGuid;
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe)
            .WithStation(Unmonitored)
            .Build();

        var result1 = fixture.Repository.UpdateChannelState(FromWe, true).ToList();

        result1.ShouldNotContain(r => r.stationId == unmonitoredId);
    }

    [TestMethod]
    public void UpdateChannelState_DisabledChannel_NotTracked()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .WithActiveChannelKeys(FromWe)
            .Build();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();

        var result = fixture.Repository.UpdateChannelState(AplWe, true).ToList();
        result.ShouldBeEmpty();
    }

    [TestMethod]
    public void UpdateChannelState_SingleChannelStation_OnlineWhenChannelConnects()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Apl1, Cargo)
            .Build();

        var result = fixture.Repository.UpdateChannelState(Cargo, true).ToList();

        result.Count.ShouldBe(1);
        result.ShouldContain((Apl1Guid, true));
    }

    [TestMethod]
    public void UpdateChannelState_DisabledChannel_StationOnlineWithRemainingChannels()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .WithStation(Ps02, FromWe, AplWe)
            .WithActiveChannelKeys(FromWe)
            .Build();

        var result = fixture.Repository.UpdateChannelState(FromWe, true).ToList();

        result.ShouldContain((Ps01Guid, true));
        result.ShouldContain((Ps02Guid, true));
    }

    [TestMethod]
    public void ForceUpdateChannelState_NeverTrackedStation_PublishesOffline()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .Build();

        var result = fixture.Repository.UpdateChannelState(FromWe, false, includeUntracked: true).ToList();

        result.Count.ShouldBe(1);
        result.ShouldContain((Ps01Guid, false));
    }

    [TestMethod]
    public void ForceUpdateChannelState_SecondChannelFails_NoDuplicateOfflineForSameStation()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .Build();

        var first = fixture.Repository.UpdateChannelState(FromWe, false, includeUntracked: true).ToList();
        var second = fixture.Repository.UpdateChannelState(AplWe, false, includeUntracked: true).ToList();

        first.ShouldContain((Ps01Guid, false));
        second.ShouldBeEmpty();
    }

    [TestMethod]
    public void ForceUpdateChannelState_AfterInitialOffline_UpdateChannelStateStillYieldsOnlineWhenAllChannelsConnect()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .Build();

        _ = fixture.Repository.UpdateChannelState(FromWe, false, includeUntracked: true).ToList();
        _ = fixture.Repository.UpdateChannelState(AplWe, false, includeUntracked: true).ToList();

        _ = fixture.Repository.UpdateChannelState(FromWe, true).ToList();
        var result = fixture.Repository.UpdateChannelState(AplWe, true).ToList();

        result.ShouldContain((Ps01Guid, true));
    }

    [TestMethod]
    public void ForceUpdateChannelState_DisabledChannel_NotTracked()
    {
        var fixture = new PlcConnectionRepositoryTestFixture()
            .WithStation(Ps01, FromWe, AplWe)
            .WithActiveChannelKeys(FromWe)
            .Build();

        var result = fixture.Repository.UpdateChannelState(AplWe, false, includeUntracked: true).ToList();

        result.ShouldBeEmpty();
    }
}