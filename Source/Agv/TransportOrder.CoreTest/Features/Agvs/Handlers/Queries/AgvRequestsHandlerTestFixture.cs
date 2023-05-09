using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

public class AgvRequestsHandlerTestFixture<T> : MassTransitTestFixture<T>
where T : class, IConsumer
{

    public readonly Mock<IAgvRegistry> AgvRegistryMock = new ();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        base.ConfigureServices(collection);
        collection.AddTransient<IAgvRegistry>(_ => AgvRegistryMock.Object);
        collection.AddTransient<IMapper>(_ => Mock.Of<IMapper>());
    }

    public async void ShouldContainFault<TMessage, TException>()
    {
        (await TestHarness!.Published.Any<Fault<TMessage>>()).ShouldBeTrue();

        IPublishedMessage<Fault<TMessage>>? fault = TestHarness.Published.Select<Fault<TMessage>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e => e.ExceptionType == typeof(TException).FullName);
    }
}