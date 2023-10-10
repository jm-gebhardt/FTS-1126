using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
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
    public async Task GetAgvsRequestHandler_RegistryEmpty_ReturnsEmptyList()
    {
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithEmptyAgvRepository()
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid>()));
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsAgv_NoActionGroup_AgvReturned()
    {
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> {AgvGenerator.GenerateIdleAgv()};
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(agvs)
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid>{agvs.First().CorrelationId}));
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
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> {agv}, actionGroup)
            .Build();

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid>{agv.CorrelationId}));
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
    public async Task GetAgvsRequestHandler_RegistryContainsMultipleAgvs_ReturnSpecificAgv()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        var correlationIds = new List<Guid>() { agv2.CorrelationId };
        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().CorrelationId.ShouldBe(agv2.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsMultipleAgvs_SpecificAgvNotFound()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv3 }) // agv2 is not added
            .Build();

        var correlationIds = new List<Guid>() { agv2.CorrelationId }; // Try to get agv2 -> should return empty result
        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsRequestHandler_RegistryContainsMultipleAgvs_ReturnMultipleAgvs()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvs, GetAgvsRequestHandler>()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv1, agv2, agv3 })
            .Build();

        // we want to get all found agvs, even if one of the requested agvs is not found. We do not want to let it throw for the whole request
        var correlationIds = new List<Guid>() { agv1.CorrelationId, agv3.CorrelationId, "INVALID".ToGuid() };
        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(correlationIds));
        response.Message.Agvs.Count.ShouldBe(2);
        response.Message.Agvs.ShouldContain(a => a.CorrelationId == agv1.CorrelationId);
        response.Message.Agvs.ShouldContain(a => a.CorrelationId == agv3.CorrelationId);
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

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid>{ agv.CorrelationId}));
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

        Response<GotAgvs> response = await fixture.GetResponseAsync<GotAgvs>(new GetAgvs(new List<Guid> { agv.CorrelationId}));
        response.Message.Agvs.Count.ShouldBe(1);
        response.Message.Agvs.First().Errors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvRequestHandler_RegistryContainsAgv_HasCurrentAction_AgvReturned()
    {
        var idleAgv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgv, GetAgvsRequestHandler>()
            .WithAgv(idleAgv, actionGroup)
            .Build();

        Response<GotAgv> response = await fixture.GetResponseAsync<GotAgv>(new GetAgv(idleAgv.CorrelationId));
        response.Message.Agv.ShouldNotBeNull();
        response.Message.Agv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
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