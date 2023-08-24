using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Commands;

[TestClass]
public class AgvCommandHandlerTest
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