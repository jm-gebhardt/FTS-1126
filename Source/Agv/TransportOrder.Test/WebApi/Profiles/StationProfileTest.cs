using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class StationProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereStationIsMappedToStationDto()
    {
        var station = new Station("testNodeId", AgvGraph.NodeType.Park, [])
        {
            Agvs = [Guid.NewGuid()],
            MapId = "map".ToGuid()
        };
        var occupantAgvId = Guid.NewGuid();
        var modifiedDate = DateTime.UtcNow;
        station.NodeLock = new NodeLock(new GraphNode(station.Id, station.Name), NodeLockState.Occupied, false, occupantAgvId, modifiedDate);
        station.SetSize(3.0, 2.0);
        var mapper = MappingHelper.GetMapper(new StationProfile());

        var stationDto = mapper.Map<StationDto>(station);

        stationDto.ShouldNotBeNull();
        stationDto.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(station.Id),
            x => x.Name.ShouldBe(station.Name),
            x => x.Length.ShouldBe(station.Length),
            x => x.Width.ShouldBe(station.Width),
            x => x.Agvs.ShouldBeEquivalentTo(station.Agvs),
            x => x.ReservedOrOccupiedBy.ShouldBe(occupantAgvId),
            x => x.MapId.ShouldBe(station.MapId),
            x => x.PrimaryType.ShouldBe(station.PrimaryType),
            x => x.AdditionalTypes.ShouldBeEquivalentTo(station.AdditionalTypes),
            x => x.State.ShouldBe(NodeLockState.Occupied),
            x => x.ModifiedDate.ShouldBe(modifiedDate),
            x => x.HasPalletJam.ShouldBe(station.HasPalletJam),
            x => x.PalletJamChangedDate.ShouldBe(station.PalletJamChangedDate));
    }

    [TestMethod]
    public void Map_WhenStationIsNull_ReturnsNull()
    {
        Station? station = null;
        var mapper = MappingHelper.GetMapper(new StationProfile());

        var stationDto = mapper.Map<StationDto>(station);

        stationDto.ShouldBeNull();
    }
}