using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

// ReSharper disable ConvertConstructorToMemberInitializers
public class ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly ActionGroupRetrievalServiceTestFixture _actionGroupRetrievalServiceTestFixture = new();
    private readonly ActionGroupRepositoryTestFixture _actionGroupRepositoryFixture = new();
    private EfGraph? _efGraph;
    private TransportOrder.Domain.Models.ActionGroup.ActionGroup[]? _actionGroups;

    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new GraphProfile());
    }

    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithActionGroups(params TransportOrder.Domain.Models.ActionGroup.ActionGroup[] actionGroups)
    {
        _actionGroups = actionGroups;

        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> AddDefaultGraph(Guid correlationId = default)
    {
        correlationId = correlationId == default ? Guid.NewGuid() : correlationId;
        var graph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var mapper = GetMapper();
        _efGraph = mapper.Map<EfGraph>(graph);

        return this;
    }

    public async Task<ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer>> BuildAsync(IBus? bus = null)
    {
        if (_actionGroups != null)
        {
            // ReSharper disable once CoVariantArrayConversion
            await _actionGroupRetrievalServiceTestFixture.AddRangeAsync(_actionGroups);
            _actionGroupRepositoryFixture.WithActionGroups(_actionGroups);
        }

        if (_efGraph != null)
        {
            await _actionGroupRetrievalServiceTestFixture.AddAsync(_efGraph);
        }
        await _actionGroupRepositoryFixture.BuildAsync();
        AddServices(s => s.AddTransient<IActionGroupRetrievalService>(_ => _actionGroupRetrievalServiceTestFixture.ActionGroupRetrievalService));
        AddServices(s => s.AddTransient<IActionGroupRepository>(_ => _actionGroupRepositoryFixture.ActionGroupRepository));
        AddServices(s => s.AddTransient<IMapper>(_ => Mock.Of<IMapper>()));
        if (bus != null)
        {
            AddServices(s =>s.AddTransient(_ => bus));
        }
        else
        {
            var busMock = new Mock<IBus>();
            AddServices(s => s.AddTransient(_ => busMock.Object));
        }
        base.Build();

        return this;
    }
}