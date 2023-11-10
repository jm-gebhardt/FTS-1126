using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Agv;

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

        const string agvId = "INVALID";
        var correlationId = agvId.ToGuid();

        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.GetAgv(correlationId));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task GetAgvGraph_ValidData_ReturnsGraphDto(int index)
    {
        var fixture = new AgvControllerTestFixture<GetAgvGraph, GetGraphRequestHandler>();
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
                e => e.FromNodeId.ShouldBe(agvGraph.Edges.ElementAt(i).FromNodeId),
                e => e.ToNodeId.ShouldBe(agvGraph.Edges.ElementAt(i).ToNodeId),
                e => e.Length.ShouldBe(agvGraph.Edges.ElementAt(i).Length));
        }
    }

    [TestMethod]
    public async Task GetAgvGraph_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<GetAgvGraph, GetGraphRequestHandler> fixture = new AgvControllerTestFixture<GetAgvGraph, GetGraphRequestHandler>()
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
        AgvControllerTestFixture<GetAgvData, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvData, GetAgvDataRequestHandler>()
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
        AgvControllerTestFixture<GetAgvData, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvData, GetAgvDataRequestHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await controller.GetAgvData(correlationId));
    }

    [TestMethod]
    public async Task AcceptAgv_WhenAgvInDiscovered_ReturnOkResult()
    {
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AgvCommandHandler>();
        var correlationId = fixture.AgvIds[2].ToGuid(); // AGV in state 'Discovered'

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        const RequestClientGenerator<AcceptAgv>.ResponseToUse responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.First;
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
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AgvCommandHandler>();
        var correlationId = fixture.AgvIds[1].ToGuid(); // AGV in state 'Busy'

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        const RequestClientGenerator<AcceptAgv>.ResponseToUse responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.Second;
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
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AgvCommandHandler>();
        var correlationId = Guid.Empty; // Invalid AGV correlationId

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        const RequestClientGenerator<AcceptAgv>.ResponseToUse responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.Third;
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
        AgvControllerTestFixture<UnlockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgv, AgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        var actionResult = await controller.UnlockAgv(correlationId);
        actionResult.ShouldNotBeNull();
        var result = actionResult as NotFoundResult;
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task UnlockAgv_WhenNotInDiscovered_UnlocksAgv()
    {
        AgvControllerTestFixture<UnlockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgv, AgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[1].ToGuid(); // AGV not in state 'Discovered'

        var result = await controller.UnlockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<UnlockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<UnlockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UnlockAgv_Ok()
    {
        AgvControllerTestFixture<UnlockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgv, AgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.UnlockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<UnlockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<UnlockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task LockAgv_Ok()
    {
        AgvControllerTestFixture<LockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<LockAgv, AgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.LockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<LockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<LockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmitRequestAgvData_NotFound()
    {
        AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestData(correlationId));
    }

    [TestMethod]
    public async Task EmitRequestAgvData_Ok()
    {
        AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.EmitRequestData(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();
        (await harness.Published.Any<RequestAgvData>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmitRequestAgvGraph_NotFound()
    {
        AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestGraph(correlationId));
    }

    [TestMethod]
    public async Task EmitRequestAgvGraph_Ok()
    {
        AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();

        var result = await controller.EmitRequestGraph(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.TestHarness!;
        (await harness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();
        (await harness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task MoveAgv_Ok()
    {
        AgvControllerTestFixture<MoveAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<MoveAgv, AgvCommandHandler>()
            .UseDefaultServices()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvIds[2].ToGuid();
        const string targetNodeId = "Test Destination Id";

        var actionResult = await controller.MoveAgv(correlationId, targetNodeId);
        var result = actionResult as OkObjectResult;
        result.ShouldNotBeNull();
        result.Value.ShouldBeOfType<Guid>();
    }

    [TestMethod]
    public async Task MoveAgv_UnknownAgv_NotFoundResult()
    {
        AgvControllerTestFixture<MoveAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<MoveAgv, AgvCommandHandler>()
            .UseDefaultServices()
            .Build();

        var controller = fixture.Controller;
        var correlationId = Guid.Empty;
        fixture.AgvRepository.Setup(x => x.GetAgvAsync(It.Is<Guid>(g => g == correlationId))).Throws(new AgvNotFoundException(correlationId));

        var actionResult = await controller.MoveAgv(correlationId, "Test Destination Id");
        actionResult.ShouldNotBeNull();
        var result = actionResult as NotFoundResult;
        result.ShouldNotBeNull();
    }
}