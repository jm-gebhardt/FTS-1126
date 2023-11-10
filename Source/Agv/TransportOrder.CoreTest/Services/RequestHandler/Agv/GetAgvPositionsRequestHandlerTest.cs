using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvPositionsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetAgvPositions_ValidData_ReturnsPositionsOfSelectedAgvs()
    {
        var positions = new List<AgvPositionStatus> {new(Guid.NewGuid(), "AgvId", new AgvPosition(true, 1, 1, 1, 1))};
        GetAgvPositionsRequestHandlerTestFixture<GetAgvPositions, GetAgvPositionsRequestHandler> fixture = new GetAgvPositionsRequestHandlerTestFixture<GetAgvPositions, GetAgvPositionsRequestHandler>()
            .WithAgvPositions(positions)
            .Build();

        Response<GotAgvPositions> response = await fixture.GetResponseAsync<GotAgvPositions>(new GetAgvPositions(positions.Select(p => p.CorrelationId).ToList()));

        List<Messages.MessageContent.AgvPositionStatus>? actualPositions = response.Message.AgvPositions;
        actualPositions.ShouldNotBeNull();
        List<AgvPositionStatus> positionsOrdered = positions.OrderBy(p => p.CorrelationId).ToList();
        actualPositions.Select(p =>
                new AgvPositionStatus(p.CorrelationId, p.AgvId, new AgvPosition(p.AgvPosition.PositionInitialized, p.AgvPosition.X, p.AgvPosition.Y, p.AgvPosition.Z, p.AgvPosition.Theta, p.AgvPosition.DeviationRange))
                {
                    CreatedDate = positionsOrdered[actualPositions.IndexOf(p)].CreatedDate
                })
            .OrderBy(p => p.CorrelationId).ShouldBe(positionsOrdered);
    }

    [TestMethod]
    public async Task Consume_GetAgvPositions_NoCorrelationIdsGiven_ReturnsEmptyList()
    {
        var positions = new List<AgvPositionStatus> {new(Guid.NewGuid(), "AgvId", new AgvPosition(true, 1, 1, 1, 1))};
        GetAgvPositionsRequestHandlerTestFixture<GetAgvPositions, GetAgvPositionsRequestHandler> fixture = new GetAgvPositionsRequestHandlerTestFixture<GetAgvPositions, GetAgvPositionsRequestHandler>()
            .WithAgvPositions(positions)
            .Build();

        Response<GotAgvPositions> response = await fixture.GetResponseAsync<GotAgvPositions>(new GetAgvPositions(new List<Guid>()));

        List<Messages.MessageContent.AgvPositionStatus>? actualPositions = response.Message.AgvPositions;
        actualPositions.ShouldNotBeNull();
        actualPositions.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetAgvPositions_WrongCorrelationId_ThrowsAgvsNotFoundException()
    {
        var positions = new List<AgvPositionStatus> {new(Guid.NewGuid(), "AgvId", new AgvPosition(true, 1, 1, 1, 1))};
        GetAgvPositionsRequestHandlerTestFixture<GetAgvPositions, GetAgvPositionsRequestHandler> fixture = new GetAgvPositionsRequestHandlerTestFixture<GetAgvPositions, GetAgvPositionsRequestHandler>()
            .WithAgvPositions(positions)
            .Build();

        List<Guid> requestedAgvs = positions.Select(p => p.CorrelationId).ToList();
        requestedAgvs.Add("INVALID".ToGuid());
        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvPositions>(new GetAgvPositions(requestedAgvs)));
    }

    [TestMethod]
    public async Task Consume_GetAgvPosition_ValidData_ReturnsAgvPosition()
    {
        var position = new AgvPositionStatus(Guid.NewGuid(), "AgvId", new AgvPosition(true, 1, 1, 1, 1));
        GetAgvPositionsRequestHandlerTestFixture<GetAgvPosition, GetAgvPositionsRequestHandler> fixture = new GetAgvPositionsRequestHandlerTestFixture<GetAgvPosition, GetAgvPositionsRequestHandler>()
            .WithAgvPositions(new List<AgvPositionStatus> {position})
            .Build();

        Response<GotAgvPosition> response = await fixture.GetResponseAsync<GotAgvPosition>(new GetAgvPosition(position.CorrelationId));
        var actualPosition = response.Message.AgvPositions;
        actualPosition.ShouldNotBeNull();
        new AgvPositionStatus(actualPosition.CorrelationId, actualPosition.AgvId, new AgvPosition(actualPosition.AgvPosition.PositionInitialized,
            actualPosition.AgvPosition.X, actualPosition.AgvPosition.Y, actualPosition.AgvPosition.Z, actualPosition.AgvPosition.Theta, actualPosition.AgvPosition.DeviationRange))
        {
            CreatedDate = position.CreatedDate
        }.ShouldBe(position);
    }

    [TestMethod]
    public async Task Consume_GetAgvPosition_WrongCorrelationId_ThrowsAgvNotFoundException()
    {
        var positions = new List<AgvPositionStatus> {new(Guid.NewGuid(), "AgvId", new AgvPosition(true, 1, 1, 1, 1))};
        GetAgvPositionsRequestHandlerTestFixture<GetAgvPosition, GetAgvPositionsRequestHandler> fixture = new GetAgvPositionsRequestHandlerTestFixture<GetAgvPosition, GetAgvPositionsRequestHandler>()
            .WithAgvPositions(positions)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvPosition>(new GetAgvPosition("INVALID".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_GetAgvPosition_EmptyRepository_ThrowsAgvNotFoundException()
    {
        GetAgvPositionsRequestHandlerTestFixture<GetAgvPosition, GetAgvPositionsRequestHandler> fixture = new GetAgvPositionsRequestHandlerTestFixture<GetAgvPosition, GetAgvPositionsRequestHandler>()
            .WithAgvPositions(new List<AgvPositionStatus>())
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvPosition>(new GetAgvPosition("ANY".ToGuid())));
    }
}