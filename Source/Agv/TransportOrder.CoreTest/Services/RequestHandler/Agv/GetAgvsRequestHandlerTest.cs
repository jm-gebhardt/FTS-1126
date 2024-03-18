using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper.AgvActionGenerator;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvsRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryEmpty_ReturnsEmptyList()
    {
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithEmptyAgvRepository()
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid>()));
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsAgv_NoActionGroup_AgvReturned()
    {
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> { AgvGenerator.GenerateIdleAgv(lastNodeId: null) };
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(agvs)
            .Build();
        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agvs.First().CorrelationId }));
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
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: myLastNodeId);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv }, actionGroup)
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
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
        respondAgv.LastNode!.Id.ShouldBe(myLastNodeId);
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvsWithLastNode_ReturnAllAgvs()
    {
        const string myLastNodeId = "myLastNodeId";
        var agv1 = AgvGenerator.GenerateIdleAgv(1, lastNodeId: myLastNodeId + "1");
        var agv2 = AgvGenerator.GenerateIdleAgv(2, lastNodeId: null);
        var agv3 = AgvGenerator.GenerateIdleAgv(3, lastNodeId: myLastNodeId + "3");
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        var correlationIds = new List<Guid>() { agv1.CorrelationId, agv2.CorrelationId, agv3.CorrelationId };
        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.Count.ShouldBe(3);
        response.Message.Agvs.First().CorrelationId.ShouldBe(agv1.CorrelationId);
        GraphNode?[] lastNodes = response.Message.Agvs.OrderBy(a => a.SerialNumber).Select(a => a.LastNode).ToArray();
        lastNodes.ShouldNotBeNull();
        lastNodes[0].ShouldNotBeNull();
        lastNodes[0]!.Id.ShouldBe(myLastNodeId + "1");
        lastNodes[1].ShouldBeNull();
        lastNodes[2].ShouldNotBeNull();
        lastNodes[2]!.Id.ShouldBe(myLastNodeId + "3");
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvsWithLastNode_ReturnSpecificAgv()
    {
        const string myLastNodeId = "myLastNodeId";
        var agv1 = AgvGenerator.GenerateIdleAgv(1, lastNodeId: myLastNodeId + "1");
        var agv2 = AgvGenerator.GenerateIdleAgv(2, lastNodeId: myLastNodeId + "2");
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        var correlationIds = new List<Guid> { agv2.CorrelationId };
        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().CorrelationId.ShouldBe(agv2.CorrelationId);
        response.Message.Agvs.First().LastNode!.Id.ShouldBe(myLastNodeId + "2");
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RepositoryContainsMultipleAgvs_SpecificAgvNotFound_ThrowsException()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
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
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
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

        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithActiveAgvErrors(agv.CorrelationId, expectedAgvErrors)
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
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
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_AgvDoesNotHaveError_ReturnsAgvWithEmptyAgvErrorList()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId }));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().Errors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvRequestHandler_RepositoryContainsAgv_HasCurrentAction_AgvReturned()
    {
        var idleAgv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .WithAgv(idleAgv, actionGroup)
            .Build();

        Response<GotAgv> response = await fixture.GetResponseAsync<GotAgv>(new GetAgv(idleAgv.CorrelationId));
        var agv = response.Message.Agv;
        agv.ShouldNotBeNull();
        agv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRequestHandler_UnknownAgvRequested_ThrowsAgvNotFoundException()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .WithAgv(agv1)
            .Build();

        var agvCorrelationId = "INVALID".ToGuid();
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgv>(new GetAgv(agvCorrelationId)));
    }
}