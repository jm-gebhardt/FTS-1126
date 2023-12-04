using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;

[TestClass]
public class StationProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereStationIsMappedToStationDto()
    {
        var station = new Station("testNodeId", 3.0, 2.0);
        var mapper = MappingHelper.GetMapper(new StationProfile());

        var stationDto = mapper.Map<StationDto>(station);

        stationDto.ShouldNotBeNull();
        stationDto.ShouldSatisfyAllConditions(
            x => x.NodeId.ShouldBe(station.Id),
            x => x.Length.ShouldBe(station.Length),
            x => x.Width.ShouldBe(station.Width));
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