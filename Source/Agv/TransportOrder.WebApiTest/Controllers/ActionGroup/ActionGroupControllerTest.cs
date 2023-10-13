using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.ActionGroup;

[TestClass]
public class ActionGroupControllerTest
{
    [TestMethod]
    public async Task GetSingleActionGroup_ValidData_ReturnsSingleActionGroupDto()
    {
        var correlationId = Guid.NewGuid();
        var actionGroup = new ActionGroupReadModel(correlationId, "OrderId", null, DateTime.UtcNow, null, null, ActionGroupState.Created, ActionGroupCreator.TransportOrder, new List<AgvActionReadModel>(), true);
        ActionGroupControllerTestFixture<GetActionGroup, ActionGroupRequestHandler> fixture = new ActionGroupControllerTestFixture<GetActionGroup, ActionGroupRequestHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;
        ActionResult<ActionGroupDto> response = await controller.GetActionGroup(actionGroup.CorrelationId);
        
        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var actionGroupDto = result.Value as ActionGroupDto;
        actionGroupDto.ShouldNotBeNull();
        actionGroupDto.CorrelationId.ShouldBe(actionGroup.CorrelationId);
    }

    [TestMethod]
    public async Task GetSingleActionGroup_UnknownCorrelationId_ReturnsNotFound()
    {
        ActionGroupControllerTestFixture<GetActionGroup, ActionGroupRequestHandler> fixture = new ActionGroupControllerTestFixture<GetActionGroup, ActionGroupRequestHandler>()
            .Setup()
            .Build();

        var correlationId = "INVALID".ToGuid();

        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () => await controller.GetActionGroup(correlationId));
    }

}