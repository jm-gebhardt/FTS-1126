using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class AgvErrorProfileTest
{
    [TestMethod]
    public void Map_WhereAgvErrorIsMappedToAgvErrorDto_ShouldMapAllPropertiesCorrectly()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId);

        var errorMessageContent = new AgvError(agvError.AgvCorrelationId, agvError.ErrorTitle, agvError.ErrorDescription, agvError.ErrorCode, agvError.ErrorLevel, agvError.ErrorType, agvError.ErrorCategory, agvError.ErrorReferences, agvError.ErrorReceived);
        var mapper = MappingHelper.GetMapper(new AgvErrorProfile());
        var agvErrorDto = mapper.Map<AgvErrorDto>(errorMessageContent);

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