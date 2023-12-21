using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.ActionGroup;

[TestClass]
public class ActionGroupControllerTest
{
    [UsedImplicitly]
    public class ActionCommandHandlerMock : IConsumer<AbortActionGroup>
    {
        public async Task Consume(ConsumeContext<AbortActionGroup> context)
        {
            await context.RespondAsync(new Success());
        }
    }

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
    
    [TestMethod]
    public async Task CancelActionGroup_ValidData_ReturnsOk()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler> fixture = new ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;

        var response = await controller.CancelActionGroup(actionGroup.CorrelationId);
        var result = response as OkResult;
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task CancelActionGroup_UnknownActionGroup_ReturnsNotFound()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler> fixture = new ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;

        var response = await controller.CancelActionGroup(Guid.NewGuid());
        var result = response as NotFoundResult;
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task CancelActionGroup_ActionGroupStarted_ReturnsBadRequest()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Started);

        ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler> fixture = new ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;

        var response = await controller.CancelActionGroup(actionGroup.CorrelationId);
        var result = response as BadRequestObjectResult;
        result.ShouldNotBeNull();
        result.Value.ShouldBe(HostOrderCancelFailedErrorCode.OrderAlreadyStarted);
    }

    [TestMethod]
    public async Task CancelActionGroup_ActionGroupCompleted_ReturnsBadRequest()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Completed);

        ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler> fixture = new ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;

        var response = await controller.CancelActionGroup(actionGroup.CorrelationId);
        var result = response as BadRequestObjectResult;
        result.ShouldNotBeNull();
        result.Value.ShouldBe(HostOrderCancelFailedErrorCode.OrderAlreadyFinished);
    }

    [TestMethod]
    public async Task CancelActionGroup_ActionGroupFailed_ReturnsBadRequest()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Failed);

        ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler> fixture = new ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;

        var response = await controller.CancelActionGroup(actionGroup.CorrelationId);
        var result = response as BadRequestObjectResult;
        result.ShouldNotBeNull();
        result.Value.ShouldBe(HostOrderCancelFailedErrorCode.OrderAlreadyFailed);
    }

    [TestMethod]
    public async Task CancelActionGroup_ActionGroupCanceled_ReturnsBadRequest()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Canceled);

        ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler> fixture = new ActionGroupControllerTestFixture<CancelActionGroup, ActionCommandHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;

        var response = await controller.CancelActionGroup(actionGroup.CorrelationId);
        var result = response as BadRequestObjectResult;
        result.ShouldNotBeNull();
        result.Value.ShouldBe(HostOrderCancelFailedErrorCode.OrderAlreadyCanceled);
    }

    [TestMethod]
    public async Task AbortActionGroup_ValidData_ReturnsOk()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Started);
        var massTransitFixture = new MassTransitConsumerTestFixture<ActionCommandHandlerMock>();
        ActionGroupControllerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture = new ActionGroupControllerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
            .Setup(massTransitFixture.TestHarness.Bus)
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;
        var response =  await controller.AbortActionGroup(actionGroup.CorrelationId);
        var result = response as OkResult;
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task AbortActionGroup_WrongActionGroupState_ReturnsBadRequest()
    {
        var actionGroup =  AgvActionGenerator.GeneratePickDropActionGroup();
        ActionGroupControllerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture = new ActionGroupControllerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
            .Setup()
            .AddActionGroup(actionGroup)
            .Build();

        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupStateInvalidException>(async () => await controller.AbortActionGroup(actionGroup.CorrelationId));
    }

    [TestMethod]
    public async Task AbortActionGroup_UnknownCorrelationId_ReturnsNotFound()
    {
        ActionGroupControllerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler> fixture = new ActionGroupControllerTestFixture<TriggerActionGroupAbortion, ActionGroupRequestHandler>()
            .Setup()
            .Build();

        var correlationId = "INVALID".ToGuid();

        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<ActionGroupNotFoundException>(async () => await controller.AbortActionGroup(correlationId));
    }
}