using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class AgvTypeProfileTest
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Map_AgvTypeReadModelToAgvTypeDto_MapsReportedByAgv(bool reportedByAgv)
    {
        var fixture = new AgvTypeProfileTestFixture();
        var readModel = new AgvTypeReadModel(
            AgvTypeProfileTestFixture.AgvTypeIdentifier.ToGuid(),
            AgvTypeProfileTestFixture.AgvTypeIdentifier,
            AgvTypeProfileTestFixture.Manufacturer,
            AgvTypeProfileTestFixture.SeriesName,
            AgvTypeProfileTestFixture.Length,
            AgvTypeProfileTestFixture.Width,
            AgvTypeProfileTestFixture.Acknowledged,
            reportedByAgv,
            AgvTypeProfileTestFixture.HasGraph,
            "");

        var dto = fixture.Mapper.Map<AgvTypeDto>(readModel);

        dto.ReportedByAgv.ShouldBe(reportedByAgv);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Map_AgvTypeInfoToAgvTypeDto_MapsReportedByAgv(bool reportedByAgv)
    {
        var fixture = new AgvTypeProfileTestFixture();
        var agvTypeInfo = AgvTypeGenerator.GenerateAgvTypeInfo(
            AgvTypeProfileTestFixture.AgvTypeIdentifier,
            AgvTypeProfileTestFixture.Manufacturer,
            AgvTypeProfileTestFixture.SeriesName,
            AgvTypeProfileTestFixture.Length,
            AgvTypeProfileTestFixture.Width,
            reportedByAgv: reportedByAgv);

        var dto = fixture.Mapper.Map<AgvTypeDto>(agvTypeInfo);

        dto.ReportedByAgv.ShouldBe(reportedByAgv);
    }
}