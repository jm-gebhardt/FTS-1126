using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Messages.Commands.Agv;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class SetForceIdeHandlerTest
{
    [TestMethod]
    public async Task SetAgvForceIdle_ExistingCorrelationId_RespondsWithCorrelationId()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var mtTestFixture = new MassTransitTestFixture();
        AgvRequestsHandlerTestFixture<SetAgvForceIdle, SetAgvForceIdleCommandHandler> fixture = new AgvRequestsHandlerTestFixture<SetAgvForceIdle, SetAgvForceIdleCommandHandler>(mtTestFixture.TestHarness.Bus)
            .WithAgv(agv)
            .Build();

        Response<SetAgvForceIdleResult> response = await fixture.GetResponseAsync<SetAgvForceIdleResult>(new SetAgvForceIdle(agv.CorrelationId));
        response.Message.CorrelationId.ShouldBe(agv.CorrelationId);
        (await mtTestFixture.TestHarness.Published.Any<SetAgvForceIdleRequested>(m => m.Context.Message == new SetAgvForceIdleRequested(agv.CorrelationId))).ShouldBeTrue();
    }

        [TestMethod]
    public async Task SetAgvForceIdle_WrongCorrelationId_ThrowsAgvNotFoundException()
    {
        var mtTestFixture = new MassTransitTestFixture();
        AgvRequestsHandlerTestFixture<SetAgvForceIdle, SetAgvForceIdleCommandHandler> fixture = new AgvRequestsHandlerTestFixture<SetAgvForceIdle, SetAgvForceIdleCommandHandler>(mtTestFixture.TestHarness.Bus)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<SetAgvForceIdleResult>(new SetAgvForceIdle("".ToGuid())));
    }
}