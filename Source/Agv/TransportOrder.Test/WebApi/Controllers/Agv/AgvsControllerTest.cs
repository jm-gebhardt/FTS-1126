using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Agv;

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
        agvs.Count.ShouldBe(fixture.AgvSerialNumbers.Length);

        for (int index = 0; index < fixture.AgvSerialNumbers.Length; index++)
        {
            string agvSerialNumber = fixture.AgvSerialNumbers[index];
            var state = fixture.States[index];

            agvs[index].CorrelationId.ShouldBe(agvSerialNumber.ToGuid());
            agvs[index].SerialNumber.ShouldBe(agvSerialNumber);

            agvs[index].CurrentState.ToString().ShouldBe(state.ToString());
            agvs[index].Errors.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public async Task GetAgvsBattery_ValidData_ReturnsAgvBattery()
    {
        AgvsControllerTestFixture<GetAgvsBattery, GetAgvBatteryRequestHandler> fixture = new AgvsControllerTestFixture<GetAgvsBattery, GetAgvBatteryRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        List<AgvBatteryStatusDto> agvBatteryStatus = (await fixture.GetControllerResultAsync(c => c.GetAgvsBattery(fixture.AgvSerialNumbers.Take(2).Select(s => s.ToGuid()).ToList()))).ToList();
        agvBatteryStatus.Count.ShouldBe(2);

        for (int index = 0; index < 2; index++)
        {
            agvBatteryStatus[index].CorrelationId.ShouldBe(fixture.AgvSerialNumbers[index].ToGuid());
        }
    }

    [TestMethod]
    public async Task GetAgvSettings_ReturnsAgvSettingsDtos()
    {
        AgvsControllerTestFixture<GetAgvsSettings, AgvSettingsRequestHandler> fixture = new AgvsControllerTestFixture<GetAgvsSettings, AgvSettingsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        List<AgvSettingsResponseDto> dtos = (await fixture.GetControllerResultAsync(c => c.GetAgvSettings(new List<Guid> { fixture.AgvSerialNumbers.Take(1).Single().ToGuid() }))).ToList();

        dtos.ShouldNotBeEmpty();
        dtos.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetAgvs_WhenAgvHasAgvError_ReturnsAgvWithAgvErrorList()
    {
        var fixture = new AgvsControllerTestFixture<GetAgvs, GetAgvsRequestHandler>();
        List<AgvError> expectedAgvErrors = fixture.AgvSerialNumbers.Select(agvId => AgvErrorGenerator.GenerateAgvError(agvId.ToGuid())).ToList();

        fixture.SetupAgvDbWithDefaultData()
            .WithActiveAgvErrors(expectedAgvErrors)
            .Build();

        List<AgvDto> agvs = (await fixture.GetControllerResultAsync(c => c.GetAgvs())).ToList();
        agvs.Count.ShouldBe(fixture.AgvSerialNumbers.Length);

        for (int index = 0; index < fixture.AgvSerialNumbers.Length; index++)
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
        var hasCargoById = new Dictionary<Guid, bool> {{Guid.NewGuid(), true}};
        AgvsControllerTestFixture<GetAgvsHasCargo, CargoRequestHandler> fixture = new AgvsControllerTestFixture<GetAgvsHasCargo, CargoRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .WithHasCargo(hasCargoById)
            .Build();

        List<AgvHasCargoDto> dto = (await fixture.GetControllerResultAsync(c => c.HasCargo(hasCargoById.Select(h => h.Key).ToList()))).ToList();
        dto.Count.ShouldBe(1);
        dto[0].CorrelationId.ShouldBe(hasCargoById.Keys.First());
        dto[0].HasCargo.ShouldBe(hasCargoById.Values.First());
    }

    [TestMethod]
    public async Task UpdateDefaultSettings_ValidData_ReturnsOk()
    {
        AgvsControllerTestFixture<UpdateDefaultAgvSettings, AgvSettingsRequestHandler> fixture = new AgvsControllerTestFixture<UpdateDefaultAgvSettings, AgvSettingsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var response = await fixture.Controller.UpdateDefaultSettings(new AgvSettingsDto(12));
        response.ShouldNotBeNull();
        var result = response as OkObjectResult;
        result.ShouldBeNull();
    }
}