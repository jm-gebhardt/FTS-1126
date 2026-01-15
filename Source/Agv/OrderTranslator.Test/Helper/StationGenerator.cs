using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;

namespace Gebhardt.Acc.OrderTranslator.Test.Helper;

/// <summary>
/// Provides stations for unit testing.
/// </summary>
public static class StationGenerator
{
    public static Station GetStation(string name,
        bool needsCargoReporting = false,
        bool needsPalletReporting = false,
        ICollection<Routing>? pickRouting = null,
        ICollection<Routing>? dropRouting = null,
        ICollection<string>? stationGroup = null,
        DoppelspielMode pickDoppelspielMode = DoppelspielMode.None,
        DoppelspielMode dropDoppelspielMode = DoppelspielMode.None,
        ICollection<string>? inputLanes = null,
        ICollection<string>? outputLanes = null
        )
    {
        return new Station(
            name,
            needsCargoReporting,
            needsPalletReporting,
            pickDoppelspielMode,
            dropDoppelspielMode,
            inputLanes ?? [],
            outputLanes ?? [],
            pickRouting ?? [],
            dropRouting ?? [],
            stationGroup ?? []);
    }
}