using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

public class AgvErrorRequestHandlerTestFixture<TRequest, TConsumer> : MediatorTestFixture<TRequest, TConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    private readonly AgvErrorRegistryTestFixture _agvErrorRegistryTestFixture;

    public AgvErrorRequestHandlerTestFixture()
    {
        _agvErrorRegistryTestFixture = new AgvErrorRegistryTestFixture();
    }

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
            new AgvGraphProfile(),
            new AgvPositionProfile(),
            new MapProfile(),
            new AgvErrorProfile()
        );
        return mapper;
    }
}