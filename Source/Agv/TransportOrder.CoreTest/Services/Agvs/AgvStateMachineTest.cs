using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Queries;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs;

[TestClass]
public class AgvStateMachineTest
{
    #region Dummy consumer
    [JetBrains.Annotations.PublicAPI]
    public class AgvPropertyChangedConsumerDummy : IConsumer<AgvPropertyChanged>
    {
        public async Task Consume(ConsumeContext<AgvPropertyChanged> context)
        {
            await Task.CompletedTask;
        }
    }
    #endregion

    #region State Transitions
    [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Idle))]
    [TestMethod]
    public async Task Transition_State_Should_Fail_When_Message_Received_SetAgvIdle(string initialState, string resultingState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);

        var message = new SetAgvIdle(agvSaga.CorrelationId);
        await fixture.TransitionStateWhenMessageReceived(message, agvSaga, resultingState);
    }

    [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging), nameof(AgvStateMachine.Idle))]
    [TestMethod]
    public async Task Transition_State_When_Message_Received_ForceReleaseAgv(string initialState, string resultingState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);

        var message = new ForceReleaseAgv(agvSaga.CorrelationId);
        await fixture.TransitionStateWhenMessageReceived(message, agvSaga, resultingState);
    }
    #endregion

    [TestMethod]
    public async Task When_AgvStatusReceived_NewAgvSagaCreated()
    {
        var fixture = new AgvStateMachineTestFixture();
        const string agvId = "newAgv";
        var correlationId = agvId.ToGuid();

        var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);

        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.Discovered.Name);
        saga.AgvId.ShouldBe(agvId);
        saga.AgvStatus.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task When_AgvStatusReceived_EmptyCorrelationId_NoSagaCreated()
    {
        var fixture = new AgvStateMachineTestFixture();

        const string agvId = "AGV#1";
        var correlationId = Guid.Empty;
        var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);

        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10))).ShouldBeNull();
    }

    [TestMethod]
    public async Task When_StateTransition_TimeOfStateTransitionUpdated()
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Discovered));
        fixture.Add(agvSaga);
        var initialTime = agvSaga.TimeOfStateTransition;
        var updatedSaga = await fixture.TransitionStateOnRequest<SetAgvIdle, SetAgvIdleSucceeded, SetAgvIdleRejected>(new SetAgvIdle(agvSaga.CorrelationId), agvSaga, nameof(AgvStateMachine.Idle));
        updatedSaga.TimeOfStateTransition.ShouldBeGreaterThan(initialTime);
    }

    [TestMethod]
    public async Task When_RejectedStateTransition_TimeOfStateTransitionNotUpdated()
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Idle));
        fixture.Add(agvSaga);
        var initialTime = agvSaga.TimeOfStateTransition;
        var updatedSaga = await fixture.TransitionStateOnRequest<SetAgvIdle, SetAgvIdleRejected, SetAgvIdleSucceeded>(new SetAgvIdle(agvSaga.CorrelationId), agvSaga, nameof(AgvStateMachine.Idle));
        updatedSaga.TimeOfStateTransition.ShouldBe(initialTime);
    }

    #region AgvNotFoundResult
    [TestMethod]
    public async Task When_RequestForUknownAgvIsReceived_ReturnNotFoundResult()
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Discovered));
        fixture.Add(agvSaga);
        var correlationId = Guid.Empty;

        await TestAgvNotFoundResult(new CanAgvTransitionToBusyRequest(correlationId, true, true), fixture);
        await TestAgvNotFoundResult(new GetAgvRequest(correlationId), fixture);
        await TestAgvNotFoundResult(new AcceptAgv(correlationId), fixture);
        await TestAgvNotFoundResult(new SetAgvIdle(correlationId), fixture);
        await TestAgvNotFoundResult(new SetAgvBusy(correlationId, true), fixture);
        await TestAgvNotFoundResult(new LockAgv(correlationId), fixture);
        await TestAgvNotFoundResult(new UnlockAgv(correlationId), fixture);
    }

    private async Task TestAgvNotFoundResult<TRequest>(TRequest request, AgvStateMachineTestFixture fixture) where TRequest : class, CorrelatedBy<Guid>
    {
        IRequestClient<TRequest>? client = fixture.TestHarness.GetRequestClient<TRequest>();
        Response<AgvNotFoundResult> response = await client.GetResponse<AgvNotFoundResult>(request);
        response.Message.ShouldBeOfType<AgvNotFoundResult>();
    }
    #endregion

    [DataRow(nameof(AgvStateMachine.Discovered))]
    [TestMethod]
    public async Task When_AcceptAgvRequest_ForDiscoveredAgv_RespondWithAcceptAgvSucceeded(string initialState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);
        await fixture.TransitionStateOnRequest<AcceptAgv, AcceptAgvSucceeded, AcceptAgvRejected>(new AcceptAgv(agvSaga.CorrelationId), agvSaga, nameof(AgvStateMachine.Idle));
    }

    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [TestMethod]
    public async Task When_AcceptAgvRequest_ForNotDiscoveredAgv_RespondWithAcceptAgvRejected(string initialState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);
        await fixture.TransitionStateOnRequest<AcceptAgv, AcceptAgvRejected, AcceptAgvSucceeded>(new AcceptAgv(agvSaga.CorrelationId), agvSaga, initialState);
    }

    [DataRow(nameof(AgvStateMachine.Discovered))]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [TestMethod]
    public async Task When_SetAgvIdleRequest_AcceptAgvSetIdle(string initialState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);
        await fixture.TransitionStateOnRequest<SetAgvIdle, SetAgvIdleSucceeded, SetAgvIdleRejected>(new SetAgvIdle(agvSaga.CorrelationId), agvSaga, nameof(AgvStateMachine.Idle));
    }

    [DataRow(nameof(AgvStateMachine.Idle))]
    [TestMethod]
    public async Task When_SetAgvIdleRequest_RejectAgvSetIdle(string initialState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);
        await fixture.TransitionStateOnRequest<SetAgvIdle, SetAgvIdleRejected, SetAgvIdleSucceeded>(new SetAgvIdle(agvSaga.CorrelationId), agvSaga, initialState);
    }

    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [TestMethod]
    public async Task When_SetAgvBusyRequest_AcceptAgvSetBusy(string initialState)
    {
        var fixture = new AgvStateMachineTestFixture()
            .WithAgvGraphExists();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);
        await fixture.TransitionStateOnRequest<SetAgvBusy, SetAgvBusySucceeded, SetAgvBusyRejected>(new SetAgvBusy(agvSaga.CorrelationId, false), agvSaga, nameof(AgvStateMachine.Busy));
    }

    [DataRow(nameof(AgvStateMachine.Discovered))]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [TestMethod]
    public async Task When_SetAgvBusyRequest_RejectAgvSetBusy(string initialState)
    {
        var fixture = new AgvStateMachineTestFixture();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(initialState);
        fixture.Add(agvSaga);
        await fixture.TransitionStateOnRequest<SetAgvBusy, SetAgvBusyRejected, SetAgvBusySucceeded>(new SetAgvBusy(agvSaga.CorrelationId, false), agvSaga, initialState);
    }

    /// <summary>
    /// Checks if AgvStatusReceived is consumed with LastNodeId
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("Node01")]
    public async Task ConsumeAgvStatusReceived_with_LastNodeId(string inputLastNode)
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture(
            busRegistrationConfigurator: x => x.AddDelayedMessageScheduler(),
            inMemoryConfiguration: (_, cfg) => cfg.UseDelayedMessageScheduler());

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false, inputLastNode)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.TestHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();
        var consumedMessage = fixture.TestHarness.Consumed.Select<AgvStatusReceived>().First().Context.Message;
        consumedMessage.AgvStatus.LastNodeId.ShouldBe(inputLastNode);

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        saga.AgvStatus.ShouldNotBeNull();
        saga.AgvStatus.LastNodeId.ShouldBe(inputLastNode);
    }

    /// <summary>
    /// Checks if requests are initially sent because AGV has no graph/data
    /// </summary>
    [TestMethod]
    public async Task When_GraphAndDataMissingInitially_BothAreRequested()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Discovered.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        );
        // Request timeout already passed
        var utcStart = DateTime.UtcNow;
        // agvSaga.AgvGraphRequestedLastTime and agvSaga.AgvDataRequestedLastTime initially will result in max timespan

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        // Publishing requests should update corresponding last requested timestamp
        ((DateTime)saga.AgvDataRequestedLastTime!).ShouldBeGreaterThan(utcStart);
        ((DateTime)saga.AgvDataRequestedLastTime!).ShouldBeLessThan(DateTime.UtcNow);
        ((DateTime)saga.AgvGraphRequestedLastTime!).ShouldBeGreaterThan(utcStart);
        ((DateTime)saga.AgvGraphRequestedLastTime!).ShouldBeLessThan(DateTime.UtcNow);
    }

    /// <summary>
    /// Checks if requests are sent because AGV has no graph/data and timeout exceeded
    /// </summary>
    [TestMethod]
    public async Task When_GraphAndDataMissingAndTimeoutExceeded_BothAreRequested()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Discovered.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        );
        // Request timeout already exceeded
        var utcStart = DateTime.UtcNow;
        // agvSaga.AgvDataRequestedLastTime is max
        const int dailyRequestUpdateInterval = 24 * 3600;
        agvSaga.AgvGraphRequestedLastTime = utcStart - new TimeSpan(0, 0, dailyRequestUpdateInterval + 1);

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        // Publishing requests should update corresponding last requested timestamp
        (saga.AgvDataRequestedLastTime > utcStart && saga.AgvDataRequestedLastTime < DateTime.UtcNow).ShouldBeTrue();
        (saga.AgvGraphRequestedLastTime > utcStart && saga.AgvGraphRequestedLastTime < DateTime.UtcNow).ShouldBeTrue();
    }

    // ReSharper disable MethodSupportsCancellation
    /// <summary>
    /// Checks if only RequestAgvData is sent because AGV already has graph
    /// </summary>
    [TestMethod]
    public async Task When_GraphIsGivenAndDataMissing_OnlyAgvDataRequested()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(5));
        var token = new CancellationTokenSource();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Discovered.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        )
        {
            // Repeat request exceeded but no daily request
            AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, AgvStateMachine.AgvRequestTimeout + 1),
            AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, AgvStateMachine.AgvRequestTimeout + 1)
        };

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        token.CancelAfter(10);
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>(token.Token)).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
    }

    // ReSharper disable MethodSupportsCancellation
    /// <summary>
    /// Checks if only RequestAgvGraph is sent because AGV already has data
    /// </summary>
    [TestMethod]
    public async Task When_DataIsGivenAndGraphMissing_OnlyAgvGraphRequested()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(5));
        var token = new CancellationTokenSource();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvData = new AgvData(correlationId, "Karis", "BF 2", null, 80, 1.2, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());
        fixture.MockAgvDataRegistry.Object.AddOrUpdate(agvData);
        const int dailyRequestUpdateInterval = 24 * 3600;

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Discovered.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        )
        {
            // Repeat request exceeded but no daily request
            AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, dailyRequestUpdateInterval + 1),
            AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, AgvStateMachine.AgvRequestTimeout + 1)
        };

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        token.CancelAfter(10);
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvData>(token.Token)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task When_AgvBatteryChargeChange_Send_AgvBatteryChargeChanged()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Idle.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        );

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, true, true, 11, true, true) // +1% battery charge
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        (await fixture.TestHarness.Published.Any<AgvBatteryChargeChanged>()).ShouldBeTrue();
    }

    /// <summary>
    /// Checks if requests are repeated once a day
    /// </summary>
    [TestMethod]
    public async Task When_GraphAndDataReceivedOneDayAgo_RequestBothAgain()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvData = new AgvData(correlationId, "Karis", "BF 2", null, 80, 1.2, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());
        fixture.MockAgvDataRegistry.Object.AddOrUpdate(agvData);

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Discovered.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        )
        {
            AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(24, 0, 1),
            AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(24, 0, 1)
        };

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
    }

    /// <summary>
    /// Checks if requests are sent in the initial state
    /// </summary>
    [TestMethod]
    public async Task When_AgvInInitialState_RequestIsDoneUnconditionally()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        const string agvId = "AGV_01";
        var correlationId = agvId.ToGuid();

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            fixture.Machine.Initial.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true)
        )
        {
            AgvGraphRequestedLastTime = DateTime.UtcNow,
            AgvDataRequestedLastTime = DateTime.UtcNow
        };

        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
            correlationId,
            agvId,
            new AgvStatus(correlationId, null, false, false, 50, true, false)
        ));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
    }

    /// <summary>
    /// Checks if RequestAgvMap is not sent as soon as the corresponding RelatedMapId is found in the registry
    /// </summary>
    [DataRow(nameof(AgvStateMachine.Discovered))]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [TestMethod]
    public async Task RequestAgvMap_NotSent_when_RelatedMapName_found_in_Registry(string previousState)
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(previousState); // The previous state is variable due to DuringAny()
        fixture.Add(agvSaga);

        var agvGraph = fixture.CreateTestAgvGraph();

        fixture.MockMapRegistry.Setup(mo => mo.Exists(agvGraph.RelatedMapId)).Returns(true);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvGraphChanged(agvSaga.CorrelationId, agvGraph));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvGraphChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvMap>()).ShouldBeFalse();
    }

    /// <summary>
    /// Checks if RequestAgvMap is received assuming that the registry is empty
    /// </summary>
    /// <remarks>When there is no match, a registry insertion must be invoked.</remarks>
    [DataRow(nameof(AgvStateMachine.Discovered))]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    [TestMethod]
    public async Task AgvMapReceived_is_Consumed_and_Processed_with_EmptyRegistry(string previousState)
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var cancellationToken = new CancellationToken();
        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(previousState); // The previous state is variable due to DuringAny()
        fixture.Add(agvSaga);

        var agvMap = fixture.CreateTestAgvMap();
        fixture.MockMapRegistry.Setup(mo => mo.AddOrUpdateAsync(agvMap.CorrelationId, agvMap.MapId, agvMap.ImageData, agvMap.Resolution, agvMap.XOffset, agvMap.YOffset, agvMap.ModificationDate, cancellationToken)).ReturnsAsync(true);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvMapReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvMap));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvMapReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Exists(agvSaga.CorrelationId)).ShouldBe(agvSaga.CorrelationId);

        fixture.MockMapRegistry.Verify(mo => mo.AddOrUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    /// <summary>
    /// Checks if RequestAgvMap is received assuming that the registry already contains a matching entry
    /// </summary>
    /// <remarks>When there is a match, any registry insertion must not happen.</remarks>
    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Discovered))]
    [DataRow(nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Busy))]
    [DataRow(nameof(AgvStateMachine.Charging))]
    public async Task AgvMapReceived_is_Consumed_and_Processed_with_Registry_containing_entries(string previousState)
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(previousState); // The previous state is variable due to DuringAny()
        fixture.Add(agvSaga);

        var agvMap = fixture.CreateTestAgvMap();
        fixture.MockMapRegistry.Setup(mo => mo.Exists(agvMap.MapId.ToGuid())).Returns(true);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvMapReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvMap));

        // Assert
        (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvMapReceived>()).ShouldBeTrue();
        (await fixture.SagaHarness.Exists(agvSaga.CorrelationId)).ShouldBe(agvSaga.CorrelationId);

        fixture.MockMapRegistry.Verify(mo => mo.AddOrUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    /// <summary>
    /// Checks if <see cref="AgvStartedCharging"/> has been consumed with required state transition to <seealso cref="AgvStateMachine.Charging"/>
    /// </summary>
    [DataRow(nameof(AgvStateMachine.Idle))]
    [TestMethod]
    public async Task AgvStartedCharging_Consumed(string previousState)
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(previousState);
        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStartedCharging(agvSaga.CorrelationId));

        // Assert
        bool consumed = await fixture.SagaHarness.Consumed.Any<AgvStartedCharging>();
        consumed.ShouldBeTrue();

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
        saga.CurrentState.ShouldBe(nameof(AgvStateMachine.Charging));
    }

    /// <summary>
    /// Checks if <see cref="AgvStoppedCharging"/> has been consumed with required state transition to <seealso cref="AgvStateMachine.Idle"/>
    /// </summary>
    [TestMethod]
    public async Task AgvStoppedCharging_Consumed()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Charging));
        fixture.Add(agvSaga);

        // Act
        await fixture.TestHarness.Bus.Publish(new AgvStoppedCharging(agvSaga.CorrelationId));

        // Assert
        bool consumed = await fixture.SagaHarness.Consumed.Any<AgvStoppedCharging>();
        consumed.ShouldBeTrue();

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
        saga.CurrentState.ShouldBe(nameof(AgvStateMachine.Idle));
    }

    [TestMethod]
    public async Task When_LockAgvReceived_AgvIsLocked()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Charging));
        var dateBefore = agvSaga.TimeOfLockStateChanged;
        fixture.Add(agvSaga);

        // Act
        await fixture.SendAndVerifyMessage(new LockAgv(agvSaga.CorrelationId));

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
        saga.TimeOfLockStateChanged.ShouldNotBe(dateBefore);
        saga.IsLocked.ShouldBeTrue();
        await fixture.VerifyMessagePublishedAsync<AgvPropertyChanged>();
    }

    [TestMethod]
    public async Task When_UnLockAgvReceived_AgvUnLocked()
    {
        // Arrange
        var fixture = new AgvStateMachineTestFixture();

        var agvSaga = AgvSagaGenerator.GenerateAgvSaga(nameof(AgvStateMachine.Charging));
        agvSaga.IsLocked = true;
        var dateBefore = agvSaga.TimeOfLockStateChanged;
        fixture.Add(agvSaga);

        // Act
        await fixture.SendAndVerifyMessage(new UnlockAgv(agvSaga.CorrelationId));

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
        saga.TimeOfLockStateChanged.ShouldNotBe(dateBefore);
        saga.IsLocked.ShouldBeFalse();
        await fixture.VerifyMessagePublishedAsync<AgvPropertyChanged>();
    }

    /// <summary>
    /// When AgvSaga state has changed, check if observer was triggered
    /// </summary>
    [TestMethod]
    public async Task When_AgvSaga_State_Changed_Trigger_Observer()
    {
        var fixture = new AgvStateMachineTestFixture()
            .WithAgvGraphExists();

        const string agvId = "AGV#1";
        var correlationId = agvId.ToGuid();
        var stateBefore = fixture.Machine.Idle;
        var stateAfter = fixture.Machine.Busy;

        var agvSaga = new AgvSaga(
            correlationId,
            agvId,
            stateBefore.Name,
            new AgvStatus(correlationId, null, true, true, 10, true, true, "R1")
        );

        fixture.Add(agvSaga);

        IRequestClient<SetAgvBusy>? client = fixture.TestHarness.GetRequestClient<SetAgvBusy>();
        Response response = await client.GetResponse<SetAgvBusySucceeded, SetAgvBusyRejected>(
            new SetAgvBusy(correlationId, false));
        response.Message.ShouldBeAssignableTo(typeof(SetAgvBusySucceeded));

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        saga.CurrentState.ShouldBe(stateAfter.Name);

        fixture.MockObserver.Verify(s => s.StateChanged(It.IsAny<BehaviorContext<AgvSaga>>(), stateAfter, stateBefore), Times.Once);
    }

    /// <summary>
    /// When watchdog expires, AGV goes offline
    /// </summary>
    [TestMethod]
    public async Task When_AgvWatchdogExpires_AgvWentOffline()
    {
        const int agvOfflineTimeoutMsWhileIdle = 100;
        const int agvOfflineTimeoutMsWhileBusy = 200;

        var fixture = new AgvStateMachineTestFixture(
            busRegistrationConfigurator: x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddConsumer<AgvPropertyChangedConsumerDummy>();
            },
            inMemoryConfiguration: (_, cfg) => cfg.UseDelayedMessageScheduler(),
            settings: new AgvSettings { AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle, AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy },
            inactivityTimeout: TimeSpan.FromMilliseconds(2 * Math.Max(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy))
        );

        const string agvId = "AGV#1";
        var correlationId = agvId.ToGuid();
        var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            { IsOnline = true };
        fixture.Add(agv);

        // send status once to activate scheduler
        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, new AgvStatus(correlationId, null, true, true, 10, true, true)));
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        // Should be online
        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeTrue();
        agv.AgvOfflineTokenId.ShouldNotBeNull();

        // Should be offline
        (await fixture.TestHarness.Sent.Any<AgvOfflineMessage>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvOfflineMessage>()).ShouldBeTrue();
        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeFalse();

        var now = DateTime.UtcNow;
        var msg = fixture.SagaHarness.Consumed.Select<AgvOfflineMessage>().First().Context.Message;
        // When triggered by AgvStatusReceived, the timeout depends on the state (and this AGV is idle)
        double duarationMin = 0.9 * agvOfflineTimeoutMsWhileBusy;
        double durationMax = 1.5 * agvOfflineTimeoutMsWhileBusy;
        double duration = (now - msg.Created).TotalMilliseconds;
        duration.ShouldBeInRange(duarationMin, durationMax);

        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeFalse();

        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();
        var propertyChangedMessage = fixture.TestHarness.Consumed.Select<AgvPropertyChanged>().First().Context.Message;
        propertyChangedMessage.Agv.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public async Task When_AlternateGameBetweenWatchdogAndStatus_AgvTogglesOfflineAndOnline()
    {
        const int agvOfflineTimeoutMsWhileIdle = 100;
        const int agvOfflineTimeoutMsWhileBusy = 200;

        var fixture = new AgvStateMachineTestFixture(
            busRegistrationConfigurator: x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddConsumer<AgvPropertyChangedConsumerDummy>();
            },
            inMemoryConfiguration: (_, cfg) => cfg.UseDelayedMessageScheduler(),
            settings: new AgvSettings { AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle, AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy },
            inactivityTimeout: TimeSpan.FromMilliseconds(2 * Math.Max(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy))
        );

        const string agvId = "AGV#1";
        var correlationId = agvId.ToGuid();
        var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, "first", false, true, 60, false, false)
            )
            { IsOnline = true };
        fixture.Add(agv);

        var agvStatus1 = new AgvStatus(correlationId, "first", false, true, 60, false, false);
        var agvStatus2 = new AgvStatus(correlationId, "second", false, true, 60, false, false);

        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus1));
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

        // Should be online
        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeTrue();
        agv.AgvOfflineTokenId.ShouldNotBeNull();

        // Should be offline
        (await fixture.TestHarness.Sent.Any<AgvOfflineMessage>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<AgvOfflineMessage>()).ShouldBeTrue();
        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeFalse();

        var now = DateTime.UtcNow;
        var msg = fixture.SagaHarness.Consumed.Select<AgvOfflineMessage>().First().Context.Message;
        // When triggered by AgvStatusReceived, the timeout depends on the state (and this AGV is idle)
        double duarationMin = 0.9 * agvOfflineTimeoutMsWhileIdle;
        double durationMax = 1.5 * agvOfflineTimeoutMsWhileIdle;
        double duration = (now - msg.Created).TotalMilliseconds;
        duration.ShouldBeInRange(duarationMin, durationMax);

        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeFalse();

        // Make sure change has been published
        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();
        var propertyChangedMessage = fixture.TestHarness.Consumed.Select<AgvPropertyChanged>().First().Context.Message;
        propertyChangedMessage.Agv.IsOnline.ShouldBeFalse();

        // Make sure nobody did spoil the test
        agvStatus1.OrderId.ShouldNotBe(agvStatus2.OrderId);

        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus2));
        // We need to wait for the "second" message (don't check just for any because a first message is already there)
        (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>(x => x.Context.Message.AgvStatus.OrderId == "second")).ShouldBeTrue();

        agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        agv.IsOnline.ShouldBeTrue();

        // Make sure second change has been published
        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        // Ignore first message with alive equals true
        (await fixture.TestHarness.Consumed.Any<AgvPropertyChanged>(x => x.Context.Message.Agv.IsOnline == false)).ShouldBeTrue();
    }
}