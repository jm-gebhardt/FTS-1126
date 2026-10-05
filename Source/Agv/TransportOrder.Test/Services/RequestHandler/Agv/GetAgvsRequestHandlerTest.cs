using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvsRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryEmpty_ReturnsEmptyList()
    {
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithEmptyAgvRepository()
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid>()));
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsAgv_NoActionGroup_AgvReturned()
    {
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> { AgvGenerator.GenerateIdleAgv(lastNodeName: null) };
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithAgvs(agvs)
            .Build();
        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agvs.First().CorrelationId }));
        response.Message.Agvs.ShouldNotBeEmpty();
        response.Message.Agvs.Count.ShouldBe(1);
        var agvDto = response.Message.Agvs.First();
        agvDto.CorrelationId.ShouldBe(agvs.First().CorrelationId);
        agvDto.SerialNumber.ShouldBe(agvs.First().SerialNumber);
        agvDto.CurrentActionGroup.ShouldBeNull();
        agvDto.LastNode.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsAgvWithLastNode_HasCurrentAction_AgvReturned()
    {
        const string myLastNodeId = "myLastNodeId";
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: myLastNodeId);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv }, actionGroup)
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
        response.Message.Agvs.ShouldNotBeEmpty();
        response.Message.Agvs.Count.ShouldBe(1);
        var respondAgv = response.Message.Agvs.First();
        respondAgv.CorrelationId.ShouldBe(agv.CorrelationId);
        respondAgv.CurrentActionGroup.ShouldNotBeNull();

        var respondActionGroup = respondAgv.CurrentActionGroup;
        respondActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        respondActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        respondActionGroup.Actions.First().ActionType.ShouldBe(actionGroup.Actions.First().ActionType);
        respondActionGroup.Actions.First().CorrelationId.ShouldBe(actionGroup.Actions.First().CorrelationId);

        respondAgv.LastNode.ShouldNotBeNull();
        respondAgv.LastNode!.Id.ShouldBe(myLastNodeId.ToGuid());
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvsWithLastNode_ReturnAllAgvs()
    {
        const string myLastNodeName1 = "myLastNodeId";
        const string myLastNodeName2 = myLastNodeName1 + "2";
        const string mzLastNodeName3 = myLastNodeName1 + "3";
        var agv1 = AgvGenerator.GenerateIdleAgv(1, lastNodeName: myLastNodeName2);
        var agv2 = AgvGenerator.GenerateIdleAgv(2, lastNodeName: null);
        var agv3 = AgvGenerator.GenerateIdleAgv(3, lastNodeName: mzLastNodeName3);
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        var correlationIds = new List<Guid>() { agv1.CorrelationId, agv2.CorrelationId, agv3.CorrelationId };
        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.Count.ShouldBe(3);
        response.Message.Agvs.First().CorrelationId.ShouldBe(agv1.CorrelationId);
        var lastNodes = response.Message.Agvs.OrderBy(a => a.SerialNumber).Select(a => a.LastNode).ToArray();
        lastNodes.ShouldNotBeNull();
        lastNodes[0].ShouldNotBeNull();
        lastNodes[0]!.Id.ShouldBe(myLastNodeName2.ToGuid());
        lastNodes[1].ShouldBeNull();
        lastNodes[2].ShouldNotBeNull();
        lastNodes[2]!.Id.ShouldBe(mzLastNodeName3.ToGuid());
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvsWithLastNode_ReturnSpecificAgv()
    {
        const string myLastNodeId1 = "myLastNodeId";
        const string myLastNodeName2 = myLastNodeId1 + "2";
        const string myLastNodeName3 = myLastNodeId1 + "3";
        var agv1 = AgvGenerator.GenerateIdleAgv(1, lastNodeName: myLastNodeName2);
        var agv2 = AgvGenerator.GenerateIdleAgv(2, lastNodeName: myLastNodeName3);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        var correlationIds = new List<Guid> { agv2.CorrelationId };
        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().CorrelationId.ShouldBe(agv2.CorrelationId);
        response.Message.Agvs.First().LastNode!.Id.ShouldBe(myLastNodeName3.ToGuid());
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvs_SpecificAgvNotFound_ThrowsException()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv3 }) // agv2 is not added
            .Build();

        var correlationIds = new List<Guid>() { agv2.CorrelationId }; // Try to get agv2 -> should return empty result
        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds)));
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvs_UnknownRequested_ThrowsException()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithLastMaintenances([])
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        // we want to get all found agvs, even if one of the requested agvs is not found. We do not want to let it throw for the whole request
        var correlationIds = new List<Guid>() { agv1.CorrelationId, agv3.CorrelationId, "INVALID".ToGuid() };
        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds)));
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_AgvHasError_ReturnsAgvWithAgvErrorList()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var expectedAgvErrors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(agv.CorrelationId)
        };

        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithLastMaintenances([])
            .WithActiveAgvErrors(agv.CorrelationId, expectedAgvErrors)
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
        response.Message.Agvs.Count.ShouldBe(1);
        foreach (var agvError in response.Message.Agvs.First().Errors)
        {
            agvError.AgvCorrelationId.ShouldBe(agvError.AgvCorrelationId);
            var expectedAgvError = expectedAgvErrors.Find(e => e.ErrorType == agvError.ErrorType);
            expectedAgvError.ShouldNotBeNull();
            agvError.ErrorDescription.ShouldBe(expectedAgvError.ErrorDescription);
            agvError.ErrorLevel.ShouldBe(expectedAgvError.ErrorLevel);
            agvError.ErrorType.ShouldBe(expectedAgvError.ErrorType);
            agvError.ErrorReferences.ShouldBe(expectedAgvError.ErrorReferences);
            agvError.ErrorReceived.ShouldBe(expectedAgvError.ErrorReceived);
            agvError.CorrelationId.ShouldBe(expectedAgvError.CorrelationId);
            agvError.ErrorResolved.ShouldBe(expectedAgvError.ErrorResolved);
        }
        response.Message.Agvs.First().LastMaintenance.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_AgvHasMaintenanceLog_ReturnsAgvWithMaintenance()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", agv.CorrelationId, DateTime.UtcNow, DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);

        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithLastMaintenances([maintenanceLog])
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().LastMaintenance.ShouldNotBeNull();
        response.Message.Agvs.First().LastMaintenance!.ExecutionDate.ShouldBe(maintenanceLog.ExecutionDate);
        response.Message.Agvs.First().LastMaintenance!.MaintenanceType.ShouldBe(maintenanceLog.MaintenanceType);
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_AgvDoesNotHaveError_ReturnsAgvWithEmptyAgvErrorList()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithLastMaintenances([])
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().Errors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvRequestHandler_RepositoryContainsAgv_HasCurrentAction_AgvReturned()
    {
        var idleAgv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog(Guid.NewGuid(), "user", idleAgv.CorrelationId, DateTime.UtcNow, DateTime.UtcNow, MaintenanceType.BatteryReplaced, null);

        var fixture = new AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .WithAgv(idleAgv, actionGroup)
            .WithLastMaintenance(maintenanceLog)
            .Build();

        var response = await fixture.GetResponseAsync<GotAgv>(new GetAgv(idleAgv.CorrelationId));
        var agv = response.Message.Agv;
        agv.ShouldNotBeNull();
        agv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
        agv.LastMaintenance.ShouldNotBeNull();
        agv.LastMaintenance.ExecutionDate.ShouldBe(maintenanceLog.ExecutionDate);
        agv.LastMaintenance.MaintenanceType.ShouldBe(maintenanceLog.MaintenanceType);
    }

    [TestMethod]
    public async Task GetAgvRequestHandler_UnknownAgvRequested_ThrowsAgvNotFoundException()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var fixture = new AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .WithAgv(agv1)
            .Build();

        var agvCorrelationId = "INVALID".ToGuid();
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgv>(new GetAgv(agvCorrelationId)));
    }
}