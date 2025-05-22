using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvErrorRequestHandlerTest
{
    [TestMethod]
    public async Task SolveRessourceBlockingErrors_ValidData_PublishesFreeNodeAndUpdateErrorStatus()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        AgvRequestsHandlerTestFixture<SolveRessourceBlockingErrors, AgvErrorRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SolveRessourceBlockingErrors, AgvErrorRequestHandler>()
            .WithAgv(agv)
            .WithActiveAgvErrors(agv.CorrelationId, [new RessourceBlockingError(agv.CorrelationId, DateTime.UtcNow, "node")])
            .WithPostion(position)
            .WithNodesStates([new NodeLock("a node", NodeLockState.Reserved, false, agv.CorrelationId, DateTime.UtcNow)])
            .Build();

        await fixture.GetResponseAsync<Success>(new SolveRessourceBlockingErrors(agv.CorrelationId));
        await fixture.ShouldHavePublishedAsync<FreeNode>();
        await fixture.ShouldHavePublishedAsync<SolveResourceErrorsReceived>();
        await fixture.ShouldHavePublishedAsync<UpdateErrorStatus>();
    }

    [TestMethod]
    public async Task SolveRessourceBlockingErrors_NoRessourceBlocked_PublishesUpdateErrorStatus()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        AgvRequestsHandlerTestFixture<SolveRessourceBlockingErrors, AgvErrorRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SolveRessourceBlockingErrors, AgvErrorRequestHandler>()
            .WithAgv(agv)
            .WithActiveAgvErrors(agv.CorrelationId, [new RessourceBlockingError(agv.CorrelationId, DateTime.UtcNow, "node")])
            .WithPostion(position)
            .WithNodesStates([])
            .Build();

        await fixture.GetResponseAsync<Success>(new SolveRessourceBlockingErrors(agv.CorrelationId));
        await fixture.ShouldNotHavePublishedAsync<FreeNode>();
        await fixture.ShouldHavePublishedAsync<SolveResourceErrorsReceived>();
        await fixture.ShouldHavePublishedAsync<UpdateErrorStatus>();
    }
}