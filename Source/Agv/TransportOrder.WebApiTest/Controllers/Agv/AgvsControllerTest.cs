using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.AspNetCore.Mvc;
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
            .SetupAgvRegistryWithDefaultData()
            .UseDefaultServices()
            .Build();

        var controller = fixture.Controller;
        ActionResult<IEnumerable<AgvDto>> response = await controller.GetAgvs();

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var agvsIterable = result.Value as IList<AgvDto>;
        agvsIterable.ShouldNotBeNull();
        List<AgvDto> agvs = agvsIterable.ToList();
        agvs.Count.ShouldBe(fixture.AgvIds.Length);

        for (int index = 0; index < fixture.AgvIds.Length; index++)
        {
            string agvId = fixture.AgvIds[index];
            var state = fixture.States[index];

            agvs[index].CorrelationId.ShouldBe(agvId.ToGuid());
            agvs[index].AgvId.ShouldBe(agvId);

            agvs[index].CurrentState.ShouldBe(state);
            agvs[index].Errors.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public async Task GetAgvs_WhenAgvHasAgvError_ReturnsAgvWithAgvErrorList()
    {
        var fixture = new AgvsControllerTestFixture<GetAgvs, GetAgvsRequestHandler>();
        List<AgvError> expectedAgvErrors = fixture.AgvIds.Select(agvId => AgvErrorGenerator.GenerateAgvError(agvId.ToGuid())).ToList();

        fixture.UseDefaultServices()
            .WithActiveAgvErrors(expectedAgvErrors)
            .Build();

        var controller = fixture.Controller;
        ActionResult<IEnumerable<AgvDto>> response = await controller.GetAgvs();

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var agvsIterable = result.Value as IList<AgvDto>;
        agvsIterable.ShouldNotBeNull();
        List<AgvDto> agvs = agvsIterable.ToList();
        agvs.Count.ShouldBe(fixture.AgvIds.Length);

        for (int index = 0; index < fixture.AgvIds.Length; index++)
        {
            agvs[index].Errors.ShouldNotBeEmpty();
            foreach (var agvDtoError in agvs[index].Errors)
            {
                agvDtoError.AgvCorrelationId.ShouldBe(agvDtoError.AgvCorrelationId);
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
}