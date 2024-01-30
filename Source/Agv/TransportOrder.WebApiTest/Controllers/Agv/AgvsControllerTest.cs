using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Agv;

[TestClass]
public class AgvsControllerTest
{
    [TestMethod]
    public async Task GetAgvs_ValidData_ReturnsAgvDtos()
    {
        AgvsControllerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvsControllerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        List<AgvDto> agvs = (await fixture.GetControllerResultAsync(c => c.GetAgvs())).ToList();
        agvs.Count.ShouldBe(fixture.AgvIds.Length);

        for (int index = 0; index < fixture.AgvIds.Length; index++)
        {
            string agvId = fixture.AgvIds[index];
            var state = fixture.States[index];

            agvs[index].CorrelationId.ShouldBe(agvId.ToGuid());
            agvs[index].AgvId.ShouldBe(agvId);

            agvs[index].CurrentState.ToString().ShouldBe(state.ToString());
            agvs[index].Errors.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public async Task GetAgvSettings_ReturnsAgvSettingsDtos()
    {
        AgvsControllerTestFixture<GetAgvsSettings, GetAgvSettingsRequestHandler> fixture = new AgvsControllerTestFixture<GetAgvsSettings, GetAgvSettingsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        List<AgvSettingsDto> dtos = (await fixture.GetControllerResultAsync(c => c.GetAgvSettings(new List<Guid> { Guid.NewGuid() }))).ToList();

        dtos.ShouldNotBeEmpty();
        dtos.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetAgvs_WhenAgvHasAgvError_ReturnsAgvWithAgvErrorList()
    {
        var fixture = new AgvsControllerTestFixture<GetAgvs, GetAgvsRequestHandler>();
        List<AgvError> expectedAgvErrors = fixture.AgvIds.Select(agvId => AgvErrorGenerator.GenerateAgvError(agvId.ToGuid())).ToList();

        fixture.SetupAgvDbWithDefaultData()
            .WithActiveAgvErrors(expectedAgvErrors)
            .Build();

        List<AgvDto> agvs = (await fixture.GetControllerResultAsync(c => c.GetAgvs())).ToList();
        agvs.Count.ShouldBe(fixture.AgvIds.Length);

        for (int index = 0; index < fixture.AgvIds.Length; index++)
        {
            agvs[index].Errors.ShouldNotBeEmpty();
            foreach (var agvDtoError in agvs[index].Errors)
            {
                var expectedAgvError = expectedAgvErrors.Find(e => e.ErrorType == agvDtoError.ErrorType);
                expectedAgvError.ShouldNotBeNull();
                agvDtoError.ErrorDescription.ShouldBe(expectedAgvError.ErrorDescription);
                agvDtoError.ErrorLevel.ShouldBe(expectedAgvError.ErrorLevel);
                agvDtoError.ErrorType.ShouldBe(expectedAgvError.ErrorType);
                agvDtoError.ErrorReferences.ShouldBe(expectedAgvError.ErrorReferences);
                agvDtoError.ErrorReceived.ShouldBe(expectedAgvError.ErrorReceived);
                agvDtoError.CorrelationId.ShouldBe(expectedAgvError.CorrelationId);
                agvDtoError.ErrorResolved.ShouldBe(expectedAgvError.ErrorResolved);
            }
        }
    }

    [TestMethod]
    public async Task GetAgvs_WhenAgvHasCargo_ReturnsAgvWithCargo()
    {
        var hasCargoById = new Dictionary<Guid, bool> { { Guid.NewGuid(), true } };
        AgvsControllerTestFixture<GetAgvsHasCargo, CargoRequestHandler> fixture = new AgvsControllerTestFixture<GetAgvsHasCargo, CargoRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .WithHasCargo(hasCargoById)
            .Build();

        List<AgvHasCargoDto> dto = (await fixture.GetControllerResultAsync(c => c.HasCargo(hasCargoById.Select(h => h.Key).ToList()))).ToList();
        dto.Count.ShouldBe(1);
        dto[0].CorrelationId.ShouldBe(hasCargoById.Keys.First());
        dto[0].HasCargo.ShouldBe(hasCargoById.Values.First());
    }
}