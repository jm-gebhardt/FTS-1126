using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Profiles;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

// ReSharper disable ConvertConstructorToMemberInitializers
public class AgvErrorRequestHandlerTestFixture<TRequest, TConsumer> : MediatorTestFixture<TRequest, TConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    private readonly AgvErrorRegistryTestFixture _agvErrorRegistryTestFixture = new();

    public AgvErrorRequestHandlerTestFixture<TRequest, TConsumer> WithAgvErrorStatus(AgvErrorStatusReceived agvErrorStatus)
    {
        _agvErrorRegistryTestFixture.WithAgvErrorStatus(agvErrorStatus);
        return this;
    }

    public async Task<AgvErrorRequestHandlerTestFixture<TRequest, TConsumer>> BuildAsync()
    {
        await _agvErrorRegistryTestFixture.BuildAsync();
        AddServices(s =>
        {
            s.AddTransient<IAgvErrorRegistry>(_ => _agvErrorRegistryTestFixture.AgvErrorRegistry);
            s.AddTransient<IMapper>(_ => GetMapper());
        });
        base.Build();
        return this;
    }

    private IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(
            new AgvProfile(),
            new AgvErrorProfile(),
            new AgvGraphProfile(),
            new MapProfile()
        );
        return mapper;
    }
}