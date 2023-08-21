using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvDtoRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvDtoRequestHandler_RegistryContainsAgv_HasCurrentAction_AgvReturned()
    {
        var idleAgv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        AgvRequestsHandlerTestFixture<GetAgvDtoRequest, GetAgvDtoRequestHandler> fixture = new GetAgvDtoRequestHandlerTestFixture()
            .WithAgvs(new List<IAgv> {idleAgv}, actionGroup)
            .Build();

        Response<GetAgvDtoResult> response = await fixture.GetResponseAsync<GetAgvDtoResult>(new GetAgvDtoRequest(idleAgv.CorrelationId));
        response.Message.Agv.ShouldNotBeNull();
        response.Message.Agv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvDtoRequestHandler_UnknownAgvRequested_ThrowsAgvNotFoundException()
    {
        var agv1 = AgvGenerator.GenerateIdleAgv(1);
        var agv2 = AgvGenerator.GenerateIdleAgv(2);
        var agv3 = AgvGenerator.GenerateIdleAgv(3);
        AgvRequestsHandlerTestFixture<GetAgvDtoRequest, GetAgvDtoRequestHandler> fixture = new GetAgvDtoRequestHandlerTestFixture()
            .WithAgvs(new List<IAgv> {agv1, agv2, agv3})
            .Build();

        var agvCorrelationId = "INVALID".ToGuid();
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GetAgvDtoResult>(new GetAgvDtoRequest(agvCorrelationId)));
    }
}