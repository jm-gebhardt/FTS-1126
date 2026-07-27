using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.RequestResponseHandler;

[TestClass]
public class SimulatedAgvRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithUnknownAgv()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () =>
            await fixture.GetResponseAsync<AgvStateResponse>(new RequestAgvState(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_KnownAgvWithoutCachedState_RespondsWithNullState()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture().Build();

        Response<AgvStateResponse> response = await fixture.GetResponseAsync<AgvStateResponse>(new RequestAgvState(Guid.NewGuid()));

        response.Message.State.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_KnownAgvWithCachedState_RespondsWithState()
    {
        var cachedState = MqttStateGenerator.Generate();
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithCachedState(cachedState)
            .Build();

        Response<AgvStateResponse> response = await fixture.GetResponseAsync<AgvStateResponse>(new RequestAgvState(Guid.NewGuid()));

        response.Message.State.ShouldBe(cachedState);
    }

    [TestMethod]
    public async Task Consume_AddLoad_InManualMode_RespondsSuccess()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithOperatingMode(AgvOperatingMode.Manual)
            .Build();

        Response<Success> response = await fixture.GetResponseAsync<RequestAddLoad, Success>(
            new RequestAddLoad(Guid.NewGuid(),
                SimulatedAgvRequestHandlerTestFixture.LoadId,
                SimulatedAgvRequestHandlerTestFixture.LoadType,
                SimulatedAgvRequestHandlerTestFixture.LoadPosition));

        response.Message.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_AddLoad_InAutomaticMode_ThrowsAgvOperatingModeNotAllowedException()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithOperatingMode(AgvOperatingMode.Automatic)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvOperatingModeNotAllowedException>(async () =>
            await fixture.GetResponseAsync<RequestAddLoad, Success>(
                new RequestAddLoad(Guid.NewGuid(),
                    SimulatedAgvRequestHandlerTestFixture.LoadId,
                    SimulatedAgvRequestHandlerTestFixture.LoadType,
                    SimulatedAgvRequestHandlerTestFixture.LoadPosition)));
    }

    [TestMethod]
    public async Task Consume_AddLoad_WhenPositionOccupied_ThrowsLoadPositionOccupiedException()
    {
        var cachedState = MqttStateGenerator.Generate(loads: [MqttStateGenerator.GenerateLoad(loadPosition: SimulatedAgvRequestHandlerTestFixture.LoadPosition)]);
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithCachedState(cachedState)
            .WithOperatingMode(AgvOperatingMode.Manual)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<LoadPositionOccupiedException>(async () =>
            await fixture.GetResponseAsync<RequestAddLoad, Success>(
                new RequestAddLoad(Guid.NewGuid(),
                    SimulatedAgvRequestHandlerTestFixture.LoadId,
                    SimulatedAgvRequestHandlerTestFixture.LoadType,
                    SimulatedAgvRequestHandlerTestFixture.LoadPosition)));
    }

    [TestMethod]
    public async Task Consume_AddLoad_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithUnknownAgv()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () =>
            await fixture.GetResponseAsync<RequestAddLoad, Success>(
                new RequestAddLoad(Guid.NewGuid(),
                    SimulatedAgvRequestHandlerTestFixture.LoadId,
                    SimulatedAgvRequestHandlerTestFixture.LoadType,
                    SimulatedAgvRequestHandlerTestFixture.LoadPosition)));
    }

    [TestMethod]
    public async Task Consume_RemoveLoad_InManualMode_WithMatchingLoad_RespondsSuccess()
    {
        var cachedState = MqttStateGenerator.Generate(loads: [MqttStateGenerator.GenerateLoad(SimulatedAgvRequestHandlerTestFixture.LoadId)]);
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithCachedState(cachedState)
            .WithOperatingMode(AgvOperatingMode.Manual)
            .Build();

        Response<Success> response = await fixture.GetResponseAsync<RequestRemoveLoad, Success>(
            new RequestRemoveLoad(Guid.NewGuid(), SimulatedAgvRequestHandlerTestFixture.LoadId));

        response.Message.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_RemoveLoad_InAutomaticMode_ThrowsAgvOperatingModeNotAllowedException()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithOperatingMode(AgvOperatingMode.Automatic)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvOperatingModeNotAllowedException>(async () =>
            await fixture.GetResponseAsync<RequestRemoveLoad, Success>(
                new RequestRemoveLoad(Guid.NewGuid(), SimulatedAgvRequestHandlerTestFixture.LoadId)));
    }

    [TestMethod]
    public async Task Consume_RemoveLoad_WhenLoadAbsent_ThrowsLoadNotFoundException()
    {
        var cachedState = MqttStateGenerator.Generate();
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithCachedState(cachedState)
            .WithOperatingMode(AgvOperatingMode.Manual)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<LoadNotFoundException>(async () => await fixture.GetResponseAsync<RequestRemoveLoad, Success>(
            new RequestRemoveLoad(Guid.NewGuid(), SimulatedAgvRequestHandlerTestFixture.LoadId)));
    }

    [TestMethod]
    public async Task Consume_RemoveLoad_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture()
            .WithUnknownAgv()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () =>
            await fixture.GetResponseAsync<RequestRemoveLoad, Success>(
                new RequestRemoveLoad(Guid.NewGuid(), SimulatedAgvRequestHandlerTestFixture.LoadId)));
    }
}