using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class AgvMaintenanceLogProfileTest
{
    [TestMethod]
    public void Map_AgvMaintenanceLogDto_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new AgvMaintenanceLogProfile());

        var log = new MaintenanceLog("user", Guid.NewGuid(), DateTime.UtcNow, MaintenanceType.TiresReplaced, "comment");

        var dto = mapper.Map<AgvMaintenanceLogDto>(log);

        dto.AgvCorrelationId.ShouldBe(log.AgvCorrelationId);
        dto.ExecutionDate.ShouldBe(log.ExecutionDate);
        dto.CreationDate.ShouldBe(log.CreationDate);
        dto.Comment.ShouldBe(log.Comment);
        dto.MaintenanceType.ShouldBe(log.MaintenanceType);
    }
}