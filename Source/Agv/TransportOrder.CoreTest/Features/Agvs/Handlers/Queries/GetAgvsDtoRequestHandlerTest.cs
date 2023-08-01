using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
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
public class GetAgvsDtoRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvsDtoRequestHandler_RegistryEmpty_ReturnsEmptyList()
    {
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .Build();

        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(new List<Guid>()));
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsDtoRequestHandler_RegistryContainsAgv_NoActionGroup_AgvReturned()
    {
        var agvs = new List<IAgv> {Mock.Of<IAgv>()};
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(agvs)
            .Build();

        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(new List<Guid>()));
        response.Message.Agvs.ShouldNotBeEmpty();
        response.Message.Agvs.Count.ShouldBe(1);
        var agvDto = response.Message.Agvs.First();
        agvDto.CorrelationId.ShouldBe(agvs.First().CorrelationId);
        agvDto.AgvId.ShouldBe(agvs.First().AgvId);
        agvDto.CurrentActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvsDtoRequestHandler_RegistryContainsAgv_HasCurrentAction_AgvReturned()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(new List<IAgv> {agv}, actionGroup)
            .Build();

        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(new List<Guid>()));
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
    public async Task GetAgvsDtoRequestHandler_RegistryContainsMultipleAgvs_ReturnSpecificAgv()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(new List<IAgv> { agv1, agv2, agv3 })
            .Build();

        var correlationIds = new List<Guid>() { agv2.CorrelationId };
        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(correlationIds));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().CorrelationId.ShouldBe(agv2.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvsDtoRequestHandler_RegistryContainsMultipleAgvs_SpecificAgvNotFound()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(new List<IAgv> { agv1, agv3 }) // agv2 is not added
            .Build();

        var correlationIds = new List<Guid>() { agv2.CorrelationId }; // Try to get agv2 -> should return empty result
        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(correlationIds));
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsDtoRequestHandler_RegistryContainsMultipleAgvs_ReturnMultipleAgvs()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(new List<IAgv> { agv1, agv2, agv3 })
            .Build();

        // we want to get all found agvs, even if one of the requested agvs is not found. We do not want to let it throw for the whole request
        var correlationIds = new List<Guid>() { agv1.CorrelationId, agv3.CorrelationId, "INVALID".ToGuid() };
        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(correlationIds));
        response.Message.Agvs.Count.ShouldBe(2);
        response.Message.Agvs.ShouldContain(a => a.CorrelationId == agv1.CorrelationId);
        response.Message.Agvs.ShouldContain(a => a.CorrelationId == agv3.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvsDtoRequestHandler_AgvHasError_ReturnsAgvWithAgvErrorList()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var expectedAgvErrors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(agv.CorrelationId)
        };

        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(new List<IAgv> { agv })
            .WithActiveAgvErrors(agv.CorrelationId, expectedAgvErrors)
            .Build();

        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(new List<Guid>()));
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
    public async Task GetAgvsDtoRequestHandler_AgvDoesNotHaveError_ReturnsAgvWithEmptyAgvErrorList()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvsDtoRequest, GetAgvsDtoRequestHandler>()
            .WithAgvs(new List<IAgv> { agv })
            .Build();

        Response<GetAgvsDtoResult> response = await fixture.GetResponseAsync<GetAgvsDtoResult>(new GetAgvsDtoRequest(new List<Guid>()));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().Errors.ShouldBeEmpty();
    }
}