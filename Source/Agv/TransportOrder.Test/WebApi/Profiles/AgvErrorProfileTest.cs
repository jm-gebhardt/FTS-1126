using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

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
            o => o.ErrorLevel.ShouldBe(agvError.ErrorLevel),
            o => o.ErrorType.ShouldBe(agvError.ErrorType),
            o => o.ErrorReceived.ShouldBe(agvError.ErrorReceived),
            o => o.ErrorDescription.ShouldBe(agvError.ErrorDescription),
            o => o.ErrorResolved.ShouldBe(agvError.ErrorResolved),
            o => o.ErrorTitle.ShouldBe(agvError.ErrorTitle),
            o => o.ErrorCode.ShouldBe(agvError.ErrorCode),
            o => o.ErrorCategory.ShouldBe(agvError.ErrorCategory)
        );
    }
}