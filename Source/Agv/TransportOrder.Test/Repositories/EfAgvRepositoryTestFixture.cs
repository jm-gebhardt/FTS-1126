using AutoMapper;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfAgvRepositoryTestFixture : TransportOrderDbFixture
{
    private readonly IMapper _mapper = MappingHelper.GetMapper(new GraphProfile());

    public AgvRepository Repository { get; }

    private readonly IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> _inMemorySettingsProvider = new InMemoryAgvSettingsProvider();

    public EfAgvRepositoryTestFixture()
    {
        Repository = new AgvRepository(ContextFactory, _inMemorySettingsProvider, _mapper);
    }

    public async Task Add(TransportOrder.Domain.Models.Agv.Agv agv, bool infiniteCharging = false)
    {
        var settings = new AgvSettingsReadModel(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            EfAgvSettingsProvider.DefaultSettings.MaxChargeUp,
            infiniteCharging);
        _inMemorySettingsProvider.Add(agv.AgvSettingsGroup, settings);
        await Repository.AddAgvAsync(agv);
    }

    public async Task AddChargingAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        await Add(agv);
        var graphNode = new GraphNode(agv.SerialNumber.ToGuid(), agv.SerialNumber) { Type = AgvGraph.NodeType.Charge };
        var node = _mapper.Map<GraphNode>(graphNode);
        await AddAsync(node);
        var nodeLock = new NodeLock(graphNode, NodeLockState.Occupied, false, agv.CorrelationId, DateTime.UtcNow);
        await AddAsync(nodeLock);
    }
}