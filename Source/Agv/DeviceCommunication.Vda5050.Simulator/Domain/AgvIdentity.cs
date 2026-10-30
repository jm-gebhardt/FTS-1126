using Gebhardt.Acc.Common.Domain.Utilities;

namespace DeviceCommunication.Vda5050.Simulator.Domain;

public record AgvIdentity(string Manufacturer, string SeriesName, string SerialNumber)
{
    public Guid Id => SerialNumber.ToGuid();

    /// <summary>
    /// The agv type identifier used for factsheet requests and caching: manufacturer.seriesName.
    /// </summary>
    public string AgvTypeIdentifier => $"{Manufacturer}.{SeriesName}";

    public override string ToString() => $"{Manufacturer}/{SerialNumber}";
}
