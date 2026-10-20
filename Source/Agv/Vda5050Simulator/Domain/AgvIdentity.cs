using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public readonly record struct AgvIdentity(string Manufacturer, string SeriesName, string SerialNumber)
{
    public Guid CorrelationId => SerialNumber.ToGuid();

    /// <summary>
    /// The agv type identifier used for factsheet requests and caching: manufacturer.seriesName.
    /// </summary>
    public string AgvTypeIdentifier => $"{Manufacturer}.{SeriesName}";

    public override string ToString() => $"{Manufacturer}/{SerialNumber}";
}
