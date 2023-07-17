using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvsRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryEmpty_ReturnsEmptyList()
    {
        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsAgv_NoActionGroup_AgvReturned()
    {
        var agvs = new List<IAgv> {Mock.Of<IAgv>()};
        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .WithAgvs(agvs)
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
        response.Message.Agvs.ShouldNotBeEmpty();
        response.Message.Agvs.Count.ShouldBe(1);
        var agvDto = response.Message.Agvs.First();
        agvDto.CorrelationId.ShouldBe(agvs.First().CorrelationId);
        agvDto.AgvId.ShouldBe(agvs.First().AgvId);
        agvDto.CurrentActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsAgv_HasCurrentAction_AgvReturned()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .WithAgvs(new List<IAgv> {agv}, actionGroup)
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
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
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_AgvHasError_ReturnsAgvWithAgvErrorList()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var expectedAgvErrors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(agv.CorrelationId)
        };

        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .WithAgvs(new List<IAgv> {agv})
            .WithActiveAgvErrors(agv.CorrelationId, expectedAgvErrors)
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
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
        AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsRequest, GetAgvsRequestHandler>()
            .WithAgvs(new List<IAgv> {agv})
            .Build();

        Response<GetAgvsResult> response = await fixture.GetResponseAsync<GetAgvsResult>(new GetAgvsRequest());
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().Errors.ShouldBeEmpty();
    }
}