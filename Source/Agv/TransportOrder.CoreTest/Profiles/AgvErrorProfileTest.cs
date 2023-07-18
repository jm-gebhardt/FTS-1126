using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class AgvErrorProfileTest
{
    [TestMethod]
    public void Map_WhereAgvErrorIsMappedToAgvErrorDto_ShouldMapAllPropertiesCorrectly()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId);

        var mapper = MappingHelper.GetMapper(new AgvErrorProfile());
        var agvErrorDto = mapper.Map<AgvErrorDto>(agvError);

        agvErrorDto.ShouldNotBeNull();
        agvErrorDto.ShouldSatisfyAllConditions(
            o => o.CorrelationId.ShouldBe(agvError.CorrelationId),
            o => o.AgvCorrelationId.ShouldBe(agvError.AgvCorrelationId),
            o => o.ErrorLevel.ShouldBe(agvError.ErrorLevel),
            o => o.ErrorType.ShouldBe(agvError.ErrorType),
            o => o.ErrorReceived.ShouldBe(agvError.ErrorReceived),
            o => o.ErrorDescription.ShouldBe(agvError.ErrorDescription),
            o => o.ErrorResolved.ShouldBe(agvError.ErrorResolved)
        );
    }
}