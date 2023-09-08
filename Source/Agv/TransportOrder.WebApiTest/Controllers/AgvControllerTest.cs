using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers;

[TestClass]
public class AgvControllerTest
{
    [TestMethod]
    public async Task GetSingleAgv_ValidData_ReturnsSingleAgvDto()
    {
        AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler> fixture = new AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .UseDefaultServices()
            .Build();

        string agvId = fixture.AgvIds[0];
        var correlationId = agvId.ToGuid();

        var controller = fixture.Controller;
        ActionResult<AgvDto> response = await controller.GetAgv(correlationId);

        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var agvDto = result.Value as AgvDto;
        agvDto.ShouldNotBeNull();
        agvDto.AgvId.ShouldBe(agvId);
    }

    [TestMethod]
    public async Task GetSingleAgv_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler> fixture = new AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .UseDefaultServices()
            .Build();

        string agvId = "INVALID";
        var correlationId = agvId.ToGuid();

        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.GetAgv(correlationId));
    }

    [TestMethod]
    public async Task GetAgvs_ValidData_ReturnsAgvDtos()
    {
        AgvControllerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvControllerTestFixture<GetAgvs, GetAgvsRequestHandler>()
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
            string state = fixture.States[index];

            agvs[index].CorrelationId.ShouldBe(agvId.ToGuid());
            agvs[index].AgvId.ShouldBe(agvId);
            Enum.TryParse(state, out AgvState agvState);
            agvs[index].CurrentState.ShouldBe(agvState);
            agvs[index].Errors.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public async Task GetAgvs_WhenAgvHasAgvError_ReturnsAgvWithAgvErrorList()
    {
        var fixture = new AgvControllerTestFixture<GetAgvs, GetAgvsRequestHandler>();
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

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task GetAgvGraph_ValidData_ReturnsGraphDto(int index)
    {
        var fixture = new AgvControllerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler>();
        var correlationId = fixture.AgvIds[index].ToGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);

        fixture
            .WithAgvGraph(correlationId, agvGraph)
            .UseDefaultServices()
            .Build();

        var controller = fixture.Controller;
        ActionResult<GraphDto> response = await controller.GetAgvGraph(correlationId);
        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var graph = result.Value as GraphDto;
        graph.ShouldNotBeNull();

        graph.CorrelationId.ShouldBe(correlationId);
        graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        graph.Edges.Count.ShouldBe(agvGraph.Edges.Count);

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            graph.Nodes[i].ShouldSatisfyAllConditions(
                n => n.Id.ShouldBe(agvGraph.Nodes.ElementAt(i).Id),
                n => n.X.ShouldBe(agvGraph.Nodes.ElementAt(i).X),
                n => n.Y.ShouldBe(agvGraph.Nodes.ElementAt(i).Y),
                n => n.Z.ShouldBe(agvGraph.Nodes.ElementAt(i).Z),
                n => n.Theta.ShouldBe(agvGraph.Nodes.ElementAt(i).Theta),
                x => x.Type.ToString().ShouldBe(agvGraph.Nodes.ElementAt(i).Type.ToString()));
        }

        for (int i = 0; i < graph.Edges.Count; i++)
        {
            graph.Edges[i].ShouldSatisfyAllConditions(
                e => e.From.Id.ShouldBe(agvGraph.Edges.ElementAt(i).FromNodeId),
                e => e.To.Id.ShouldBe(agvGraph.Edges.ElementAt(i).ToNodeId));
        }
    }

    [TestMethod]
    public async Task GetAgvGraph_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler> fixture = new AgvControllerTestFixture<GetAgvGraph, GetAgvGraphRequestHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.GetAgvGraph(correlationId));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task GetAgvData_ValidData_ReturnsAgvDataDto(int index)
    {
        AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[index].ToGuid();
        ActionResult<AgvDataDto> response = await controller.GetAgvData(correlationId);
        var result = response.Result as OkObjectResult;
        result.ShouldNotBeNull();

        var agvData = result.Value as AgvDataDto;
        agvData.ShouldNotBeNull();

        agvData.CorrelationId.ShouldBe(correlationId);
        agvData.IpV4Address.ShouldBe(fixture.Ips[index]);
    }

    [TestMethod]
    public async Task GetAgvData_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvDataRequest, GetAgvDataRequestHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await controller.GetAgvData(correlationId));
    }

    [TestMethod]
    public async Task AcceptAgv_WhenAgvInDiscovered_ReturnOkResult()
    {
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AcceptAgvCommandHandler>();
        var correlationId = fixture.AgvIds[2].ToGuid(); // AGV in state 'Discovered'

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        var responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.First;
        IRequestClient<AcceptAgv> requestClient = RequestClientGenerator<AcceptAgv>.CreateDummyRequestClient(response1, response2, response3, responseToUse);

        fixture.AddService(sc => sc.AddTransient(_ => requestClient))
            .UseDefaultServices()
            .Build();

        var result = await fixture.Controller.AcceptAgv(correlationId);
        result.ShouldBeAssignableTo<OkResult>();
    }

    [TestMethod]
    public async Task AcceptAgv_WhenAgvNotInDiscovered_ReturnConflictResult()
    {
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AcceptAgvCommandHandler>();
        var correlationId = fixture.AgvIds[1].ToGuid(); // AGV in state 'Busy'

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        var responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.Second;
        IRequestClient<AcceptAgv> requestClient = RequestClientGenerator<AcceptAgv>.CreateDummyRequestClient(response1, response2, response3, responseToUse);

        fixture.AddService(sc => sc.AddTransient(_ => requestClient))
            .UseDefaultServices()
            .Build();

        var result = await fixture.Controller.AcceptAgv(correlationId);
        result.ShouldBeAssignableTo<ConflictResult>();
    }

    [TestMethod]
    public async Task AcceptAgv_UnknownAgv_ReturnNotFoundResult()
    {
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AcceptAgvCommandHandler>();
        var correlationId = Guid.Empty; // Invalid AGV correlationId

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        var responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.Third;
        IRequestClient<AcceptAgv> requestClient = RequestClientGenerator<AcceptAgv>.CreateDummyRequestClient(response1, response2, response3, responseToUse);

        fixture.AddService(sc => sc.AddTransient(_ => requestClient))
            .UseDefaultServices()
            .Build();

        var result = await fixture.Controller.AcceptAgv(correlationId);
        result.ShouldBeAssignableTo<NotFoundResult>();
    }

    [TestMethod]
    public async Task UnlockAgv_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.UnlockAgv, LockAndUnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.UnlockAgv, LockAndUnlockAgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.UnlockAgv(correlationId));
    }

    [TestMethod]
    public async Task UnlockAgv_WhenNotInDiscovered_UnlocksAgv()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.UnlockAgv, LockAndUnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.UnlockAgv, LockAndUnlockAgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[1].ToGuid(); // AGV not in state 'Discovered'

        var result = await controller.UnlockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<global::TransportOrder.Messages.Commands.Agv.UnlockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<global::TransportOrder.Messages.Commands.Agv.UnlockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UnlockAgv_Ok()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.UnlockAgv, LockAndUnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.UnlockAgv, LockAndUnlockAgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.UnlockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<global::TransportOrder.Messages.Commands.Agv.UnlockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<global::TransportOrder.Messages.Commands.Agv.UnlockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task LockAgv_WhenAgvRegistryThrows_ThrowsAgvNotFoundException()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.LockAgv, LockAndUnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.LockAgv, LockAndUnlockAgvCommandHandler>()
            .UseDefaultServices()
            .Build();

        var controller = fixture.Controller;
        var correlationId = Guid.Empty;
        fixture.AgvRegistry.Setup(x => x.GetRequiredAgv(It.Is<Guid>(g => g == correlationId))).Returns(() => throw new AgvNotFoundException(correlationId));

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.LockAgv(correlationId));
    }

    [TestMethod]
    public async Task LockAgv_Ok()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.LockAgv, LockAndUnlockAgvCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.LockAgv, LockAndUnlockAgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.LockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<global::TransportOrder.Messages.Commands.Agv.LockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<global::TransportOrder.Messages.Commands.Agv.LockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmitRequestAgvData_NotFound()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvData, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvData, RequestAgvDataCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestData(correlationId));
    }

    [TestMethod]
    public async Task EmitRequestAgvData_Ok()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvData, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvData, RequestAgvDataCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.EmitRequestData(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<global::TransportOrder.Messages.Commands.Agv.RequestAgvData>()).ShouldBeTrue();
        (await harness.Published.Any<global::TransportOrder.Messages.Commands.Agv.RequestAgvData>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmitRequestAgvGraph_NotFound()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph, RequestAgvGraphCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestGraph(correlationId));
    }

    [TestMethod]
    public async Task EmitRequestAgvGraph_Ok()
    {
        AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph, RequestAgvGraphCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.EmitRequestGraph(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph>()).ShouldBeTrue();
        (await harness.Published.Any<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task MoveAgv_Ok()
    {
        AgvControllerTestFixture<MoveAgv, MoveAgvCommandHandler> fixture = new AgvControllerTestFixture<MoveAgv, MoveAgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();
        const string targetNodeId = "Test Destination Id";

        var actionResult = await controller.MoveAgv(correlationId, targetNodeId);
        var result = actionResult as OkObjectResult;
        result.ShouldNotBeNull();
        result.Value.ShouldBeOfType<Guid>();

        var actionGroup = fixture.SentQueueActionGroup;
        actionGroup.ShouldNotBeNull();
        actionGroup.AssignedAgv.ShouldBe(correlationId);
        actionGroup.IsManual.ShouldBeTrue();
        actionGroup.Actions.Count.ShouldBe(1);
        actionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
        var action = actionGroup.Actions.Single();
        action.ActionType.ShouldBe(ActionType.Move);
        action.TargetNodeId.ShouldBe(targetNodeId);
    }

    [TestMethod]
    public async Task MoveAgv_WhenAgvRegistryThrows_ThrowsAgvNotFoundException()
    {
        AgvControllerTestFixture<MoveAgv, MoveAgvCommandHandler> fixture = new AgvControllerTestFixture<MoveAgv, MoveAgvCommandHandler>()
            .UseDefaultServices()
            .Build();

        var controller = fixture.Controller;
        var correlationId = Guid.Empty;
        fixture.AgvRegistry.Setup(x => x.GetRequiredAgv(It.Is<Guid>(g => g == correlationId))).Returns(() => throw new AgvNotFoundException(correlationId));

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.MoveAgv(correlationId, "Test Destination Id"));
    }
}