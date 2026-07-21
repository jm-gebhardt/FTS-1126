using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Testing.Helpers;
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
        var fixture = SimulatedAgvRequestHandlerTestFixture.WithUnknownAgv();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<AgvStateResponse>(new RequestAgvState(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_KnownAgvWithoutCachedState_RespondsWithNullState()
    {
        var fixture = new SimulatedAgvRequestHandlerTestFixture();

        Response<AgvStateResponse> response = await fixture.GetResponseAsync<AgvStateResponse>(new RequestAgvState(Guid.NewGuid()));

        response.Message.State.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_KnownAgvWithCachedState_RespondsWithState()
    {
        var cachedState = MqttStateGenerator.Generate();
        var fixture = SimulatedAgvRequestHandlerTestFixture.WithCachedState(cachedState);

        Response<AgvStateResponse> response = await fixture.GetResponseAsync<AgvStateResponse>(new RequestAgvState(Guid.NewGuid()));

        response.Message.State.ShouldBe(cachedState);
    }
}