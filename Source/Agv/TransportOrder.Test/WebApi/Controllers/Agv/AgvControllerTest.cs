using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Agv;

[TestClass]
public class AgvControllerTest
{
    [TestMethod]
    public async Task GetSingleAgv_ValidData_ReturnsSingleAgvDto()
    {
        var fixture = new AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler>();

        string agvSerialNumber = fixture.AgvSerialNumbers[0];
        var correlationId = agvSerialNumber.ToGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);

        fixture
            .WithAgvGraph(correlationId, agvGraph)
            .SetupAgvDbWithDefaultData()
            .Build();

        var agvDto = await fixture.GetControllerResultAsync(c => c.GetAgv(correlationId));

        var expectedAgvDto = new AgvDto(correlationId, agvSerialNumber, agvSerialNumber, AgvStateType.Idle, agvDto.TimeOfStateTransition, DateTime.MinValue, false, false, true, true, 10d, true, null, new List<AgvErrorDto>(), new GraphNodeDto {Id = "WAY1", Type = AgvGraph.NodeType.WayPoint, X = 1.0f, Y = 2.0f, Z = 0.0f, Theta = 0.0f});
        agvDto.ShouldNotBeNull();
        agvDto.CorrelationId.ShouldBe(expectedAgvDto.CorrelationId);
        agvDto.SerialNumber.ShouldBe(expectedAgvDto.SerialNumber);
        agvDto.BatteryCharge.ShouldBe(expectedAgvDto.BatteryCharge);
        agvDto.IsCharging.ShouldBe(expectedAgvDto.IsCharging);
        agvDto.IsLocked.ShouldBe(expectedAgvDto.IsLocked);
        agvDto.IsOnline.ShouldBe(expectedAgvDto.IsOnline);
        agvDto.IsDisturbed.ShouldBe(expectedAgvDto.IsDisturbed);
        agvDto.IsAutomatic.ShouldBe(expectedAgvDto.IsAutomatic);
        agvDto.IsLocked.ShouldBe(expectedAgvDto.IsLocked);
        agvDto.CurrentState.ShouldBe(expectedAgvDto.CurrentState);
        agvDto.CurrentActionGroup.ShouldBe(expectedAgvDto.CurrentActionGroup);
        agvDto.Errors.ShouldBe(expectedAgvDto.Errors);
        agvDto.TimeOfLockStateChanged.ShouldBe(DateTime.MinValue);
        agvDto.LastNode.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetSingleAgv_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler> fixture = new AgvControllerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        const string agvSerialNumber = "INVALID";
        var correlationId = agvSerialNumber.ToGuid();

        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.GetAgv(correlationId));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task GetAgvGraph_ValidData_ReturnsGraphDto(int index)
    {
        var fixture = new AgvControllerTestFixture<GetAgvGraph, GetGraphRequestHandler>();
        var correlationId = fixture.AgvSerialNumbers[index].ToGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);

        fixture
            .WithAgvGraph(correlationId, agvGraph)
            .SetupAgvDbWithDefaultData()
            .Build();

        var graph = await fixture.GetControllerResultAsync(c => c.GetAgvGraph(correlationId));

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
            .SetupAgvDbWithDefaultData()
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
            .SetupAgvDbWithDefaultData()
            .Build();
        var correlationId = fixture.AgvSerialNumbers[index].ToGuid();

        var agvData = await fixture.GetControllerResultAsync(c => c.GetAgvData(correlationId));
        agvData.ShouldNotBeNull();

        agvData.CorrelationId.ShouldBe(correlationId);
        agvData.IpV4Address.ShouldBe(fixture.Ips[index]);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task GetAgvBattery_ValidData_ReturnsAgvBatteryStatusDto(int index)
    {
        AgvControllerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler> fixture = new AgvControllerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var correlationId = fixture.AgvSerialNumbers[index].ToGuid();

        var agvData = await fixture.GetControllerResultAsync(c => c.GetAgvBattery(correlationId));
        agvData.ShouldNotBeNull();

        agvData.CorrelationId.ShouldBe(correlationId);
    }

    [TestMethod]
    public async Task GetAgvBattery_UnknownCorrelationId_ThrowsException()
    {
        AgvControllerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler> fixture = new AgvControllerTestFixture<GetAgvBattery, GetAgvBatteryRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetControllerResultAsync(c => c.GetAgvBattery(Guid.Empty)));
    }

    [TestMethod]
    public async Task GetAgvSettings_ValidData_ReturnsAgvSettingsDto()
    {
        AgvControllerTestFixture<GetAgvSettings, AgvSettingsRequestHandler> fixture = new AgvControllerTestFixture<GetAgvSettings, AgvSettingsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var correlationId = fixture.AgvSerialNumbers[0].ToGuid();
        var dto = await fixture.GetControllerResultAsync(c => c.GetAgvSettings(correlationId));

        dto.CorrelationId.ShouldBe(correlationId);
        dto.BatteryLevelCritical.ShouldBe(fixture.AgvSettingsReadModel.BatteryLevelCritical);
        dto.BatteryLevelLow.ShouldBe(fixture.AgvSettingsReadModel.BatteryLevelLow);
        dto.MinChargeUp.ShouldBe(fixture.AgvSettingsReadModel.MinChargeUp);
        dto.MaxChargeUp.ShouldBe(fixture.AgvSettingsReadModel.MaxChargeUp);
    }

    [TestMethod]
    public async Task GetAgvData_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<GetAgvData, GetAgvDataRequestHandler> fixture = new AgvControllerTestFixture<GetAgvData, GetAgvDataRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvDataNotFoundException>(async () => await controller.GetAgvData(correlationId));
    }

    [TestMethod]
    public async Task AcceptAgv_WhenAgvInDiscovered_ReturnOkResult()
    {
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AgvCommandHandler>();
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid(); // AGV in state 'Discovered'

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        const RequestClientGenerator<AcceptAgv>.ResponseToUse responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.First;
        IRequestClient<AcceptAgv> requestClient = RequestClientGenerator<AcceptAgv>.CreateDummyRequestClient(response1, response2, response3, responseToUse);

        fixture.AddService(sc => sc.AddTransient(_ => requestClient))
            .SetupAgvDbWithDefaultData()
            .Build();

        var result = await fixture.Controller.AcceptAgv(correlationId);
        result.ShouldBeAssignableTo<OkResult>();
    }

    [TestMethod]
    public async Task AcceptAgv_WhenAgvNotInDiscovered_ReturnConflictResult()
    {
        var fixture = new AgvControllerTestFixture<AcceptAgvCommand, AgvCommandHandler>();
        var correlationId = fixture.AgvSerialNumbers[1].ToGuid(); // AGV in state 'Busy'

        var response1 = new AcceptAgvSucceeded(correlationId);
        var response2 = new AcceptAgvRejected(correlationId);
        var response3 = new AgvNotFoundResult(correlationId);
        const RequestClientGenerator<AcceptAgv>.ResponseToUse responseToUse = RequestClientGenerator<AcceptAgv>.ResponseToUse.Second;
        IRequestClient<AcceptAgv> requestClient = RequestClientGenerator<AcceptAgv>.CreateDummyRequestClient(response1, response2, response3, responseToUse);

        fixture.AddService(sc => sc.AddTransient(_ => requestClient))
            .SetupAgvDbWithDefaultData()
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
            .SetupAgvDbWithDefaultData()
            .Build();

        var result = await fixture.Controller.AcceptAgv(correlationId);
        result.ShouldBeAssignableTo<NotFoundResult>();
    }

    [TestMethod]
    public async Task UnlockAgv_UnknownCorrelationId_ReturnsNotFound()
    {
        AgvControllerTestFixture<UnlockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgv, AgvCommandHandler>()
            .SetupAgvDbWithDefaultData()
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
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvSerialNumbers[1].ToGuid(); // AGV not in state 'Discovered'

        var result = await controller.UnlockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;
        (await harness.Consumed.Any<UnlockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<UnlockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UnlockAgv_Ok()
    {
        AgvControllerTestFixture<UnlockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<UnlockAgv, AgvCommandHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var result = await controller.UnlockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;
        (await harness.Consumed.Any<UnlockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<UnlockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task LockAgv_Ok()
    {
        AgvControllerTestFixture<LockAgv, AgvCommandHandler> fixture = new AgvControllerTestFixture<LockAgv, AgvCommandHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var result = await controller.LockAgv(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;
        (await harness.Consumed.Any<LockAgv>()).ShouldBeTrue();
        (await harness.Published.Any<LockAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmitRequestAgvData_NotFound()
    {
        AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestData(correlationId));
    }

    [TestMethod]
    public async Task EmitRequestAgvData_Ok()
    {
        AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvData, RequestAgvDataCommandHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var result = await controller.EmitRequestData(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;
        (await harness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();
        (await harness.Published.Any<RequestAgvData>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmitRequestAgvGraph_NotFound()
    {
        AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = Guid.Empty;

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.EmitRequestGraph(correlationId));
    }

    [TestMethod]
    public async Task EmitRequestAgvGraph_Ok()
    {
        AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler> fixture = new AgvControllerTestFixture<RequestAgvGraph, RequestAgvGraphCommandHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var result = await controller.EmitRequestGraph(correlationId) as OkResult;
        result.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;
        (await harness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();
        (await harness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task MoveAgv_Ok()
    {
        var targetNode = new GraphNode("target");
        AgvControllerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new AgvControllerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .SetupAgvDbWitNode(targetNode)
            .Build();
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var result = await fixture.GetControllerResultAsync<Guid, OkObjectResult>(c => c.MoveAgv(correlationId, targetNode.Id));
        result.Value.ShouldBeOfType<Guid>();
    }

    [TestMethod]
    public async Task ChargeAgvAtCharger_Ok()
    {
        var targetNode = new GraphNode("charger") { Type = AgvGraph.NodeType.Charge };
        AgvControllerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new AgvControllerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .SetupAgvDbWitNode(targetNode)
            .Build();
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var result = await fixture.GetControllerResultAsync<Guid, OkObjectResult>(c => c.ChargeAgvAtCharger(correlationId, targetNode.Id));
        result.Value.ShouldBeOfType<Guid>();
    }

    [TestMethod]
    public async Task GetAgvs_WhenAgvHasCargo_ReturnsAgvWithCargo()
    {
        AgvControllerTestFixture<GetAgvHasCargo, CargoRequestHandler> fixture = new AgvControllerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();

        var agvCorrelationId = fixture.AgvSerialNumbers[0].ToGuid();
        fixture.WithHasCargo(agvCorrelationId, true);

        bool dto = await fixture.GetControllerResultValueAsync(c => c.HasCargo(agvCorrelationId));
        dto.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SendAgvToCharger_Ok()
    {
        AgvControllerTestFixture<SendAgvToCharger, SendAgvToChargerRequestHandler> fixture = new AgvControllerTestFixture<SendAgvToCharger, SendAgvToChargerRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var controller = fixture.Controller;
        var correlationId = fixture.AgvSerialNumbers[2].ToGuid();

        var actionResult = await controller.SendAgvToCharger(correlationId);
        var result = actionResult as OkObjectResult;
        result.ShouldBeAssignableTo<OkResult>();
    }

    [TestMethod]
    public async Task SendAgvToCharger_UnknownAgv_NotFoundResult()
    {
        AgvControllerTestFixture<SendAgvToCharger, SendAgvToChargerRequestHandler> fixture = new AgvControllerTestFixture<SendAgvToCharger, SendAgvToChargerRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var correlationId = Guid.Empty;
        fixture.AgvDb.Setup(x => x.Agvs.GetAgvAsync(It.Is<Guid>(g => g == correlationId))).Throws(new AgvNotFoundException(correlationId));
        var controller = fixture.Controller;
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await controller.SendAgvToCharger(correlationId));
    }

    [TestMethod]
    public async Task UpdateAgvSettings_ValidData_Ok()
    {
        AgvControllerTestFixture<UpdateAgvSettings, AgvSettingsRequestHandler> fixture = new AgvControllerTestFixture<UpdateAgvSettings, AgvSettingsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var actionResult = await fixture.Controller.UpdateAgvSettings(fixture.AgvSerialNumbers[2].ToGuid(), new AgvSettingsDto(batteryLevelCritical:13));
        var result = actionResult as OkObjectResult;
        result.ShouldBeAssignableTo<OkResult>();
    }

    [TestMethod]
    public async Task ResetAgvSettingToDefault_ValidData_Ok()
    {
        AgvControllerTestFixture<ResetAgvSettingToDefault, AgvSettingsRequestHandler> fixture = new AgvControllerTestFixture<ResetAgvSettingToDefault, AgvSettingsRequestHandler>()
            .SetupAgvDbWithDefaultData()
            .Build();
        var actionResult = await fixture.Controller.ResetAgvSettingToDefault(fixture.AgvSerialNumbers[2].ToGuid(), "BatterLevelCritical");
        var result = actionResult as OkObjectResult;
        result.ShouldBeAssignableTo<OkResult>();
    }
}