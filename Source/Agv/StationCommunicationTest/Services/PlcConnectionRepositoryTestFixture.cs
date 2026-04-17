using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.StationCommunication.Test.Services;

internal class PlcConnectionRepositoryTestFixture
{
    public const string Ps01 = "PS01";
    public static readonly Guid Ps01Guid = Ps01.ToGuid();
    public const string Ps02 = "PS02";
    public static readonly Guid Ps02Guid = Ps02.ToGuid();
    public const string Apl1 = "APL1";
    public static readonly Guid Apl1Guid = Apl1.ToGuid();
    public const string Unmonitored = "UNMONITORED";
    public static readonly Guid UnmonitoredGuid = Unmonitored.ToGuid();

    public const string FromWe = ServiceKeyProvider.FromWeStation;
    public const string AplWe = ServiceKeyProvider.AplWeStation;
    public const string Cargo = ServiceKeyProvider.AgvCargoState;

    private readonly List<Station> _stations = [];
    private List<string>? _activeChannelKeys;

    public IPlcConnectionRepository Repository { get; private set; } = null!;

    public PlcConnectionRepositoryTestFixture WithStation(string name, params string[] plcConnections)
    {
        _stations.Add(new Station(name, false) { PlcConnections = plcConnections });
        return this;
    }

    public PlcConnectionRepositoryTestFixture WithActiveChannelKeys(params string[] keys)
    {
        _activeChannelKeys = [..keys];
        return this;
    }

    public PlcConnectionRepositoryTestFixture Build()
    {
        var repository = new PlcConnectionRepository(
            new ConcurrentDictionaryProvider<string, bool>(),
            new ConcurrentDictionaryProvider<Guid, bool>());
        List<string> keys = _activeChannelKeys ?? _stations.SelectMany(s => s.PlcConnections).Distinct().ToList();
        repository.Initialize(_stations, keys);
        Repository = repository;
        return this;
    }
}