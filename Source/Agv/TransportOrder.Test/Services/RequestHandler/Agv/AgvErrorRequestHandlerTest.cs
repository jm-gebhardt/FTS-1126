using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvErrorRequestHandlerTest
{
    [TestMethod]
    public async Task SolveResourceBlockingErrors_ValidData_PublishesFreeNodeAndResolveSystemAgvError()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        AgvRequestsHandlerTestFixture<SolveResourceBlockingErrors, AgvErrorRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SolveResourceBlockingErrors, AgvErrorRequestHandler>()
            .WithAgv(agv)
            .WithActiveAgvErrors(agv.CorrelationId, [new ResourceBlockingError(agv.CorrelationId, DateTime.UtcNow, "node")])
            .WithPostion(position)
            .WithNodesStates([new NodeLock("a node", NodeLockState.Reserved, false, agv.CorrelationId, DateTime.UtcNow)])
            .Build();

        await fixture.GetResponseAsync<Success>(new SolveResourceBlockingErrors(agv.CorrelationId));
        await fixture.ShouldHavePublishedAsync<FreeNode>();
        await fixture.ShouldHavePublishedAsync<SolveResourceErrorsReceived>();
        await fixture.ShouldHavePublishedAsync<ResolveSystemAgvError>();
        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromQueue>();
    }

    [TestMethod]
    public async Task SolveResourceBlockingErrors_SolveResourceBlocking_PublishesResolveSystemAgvError()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        AgvRequestsHandlerTestFixture<SolveResourceBlockingErrors, AgvErrorRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SolveResourceBlockingErrors, AgvErrorRequestHandler>()
            .WithAgv(agv)
            .WithActiveAgvErrors(agv.CorrelationId, [new ResourceBlockingError(agv.CorrelationId, DateTime.UtcNow, "node")])
            .WithPostion(position)
            .WithNodesStates([])
            .Build();

        await fixture.GetResponseAsync<Success>(new SolveResourceBlockingErrors(agv.CorrelationId));
        await fixture.ShouldNotHavePublishedAsync<FreeNode>();
        await fixture.ShouldHavePublishedAsync<SolveResourceErrorsReceived>();
        await fixture.ShouldHavePublishedAsync<ResolveSystemAgvError>();
        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromQueue>();
    }

    [TestMethod]
    public async Task SolveResourceBlockingErrors_QueueBlockingError_PublishesRemoveAgvFromQueue()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        AgvRequestsHandlerTestFixture<SolveResourceBlockingErrors, AgvErrorRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SolveResourceBlockingErrors, AgvErrorRequestHandler>()
            .WithAgv(agv)
            .WithActiveAgvErrors(agv.CorrelationId, [new QueueBlockingError(agv.CorrelationId, DateTime.UtcNow)])
            .WithPostion(position)
            .WithNodesStates([])
            .Build();

        await fixture.GetResponseAsync<Success>(new SolveResourceBlockingErrors(agv.CorrelationId, IgnoreErrorState:true));
        await fixture.ShouldNotHavePublishedAsync<FreeNode>();
        await fixture.ShouldHavePublishedAsync<SolveResourceErrorsReceived>();
        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromQueue>();
    }
}