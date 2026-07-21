using AutoMapper;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Testing.Filters;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Consumer;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Consumer;

public class SimulatedAgvStateChangedConsumerTestFixture : MassTransitConsumerTestFixture<SimulatedAgvStateChangedConsumer>
{
    private readonly Mock<ISimulatedAgvStatePublisher> _publisherMock = new();
    public const string SerialNumber = "simagv3";

    public Vda5050StateDto? PublishedState { get; private set; }

    public SimulatedAgvStateChangedConsumerTestFixture(bool shouldPublish = true)
    {
        _publisherMock
            .Setup(p => p.SendStateChangedAsync(It.IsAny<Vda5050StateDto>()))
            .Callback<Vda5050StateDto>(dto => PublishedState = dto)
            .Returns(Task.CompletedTask);

        AddServices(s =>
        {
            s.AddSingleton<IMessageFilter<Vda5050StateDto>>(new FilterMock<Vda5050StateDto>(shouldPublish));
            s.AddSingleton(_publisherMock.Object);
        });
        Build();
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [new Vda5050StateProfile()];
    }

    public void VerifyPublished(Times times)
    {
        _publisherMock.Verify(p => p.SendStateChangedAsync(It.IsAny<Vda5050StateDto>()), times);
    }
}