using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public readonly record struct AgvId(string Manufacturer, string SerialNumber)
{
    public Guid CorrelationId => SerialNumber.ToGuid();

    public override string ToString() => $"{Manufacturer}/{SerialNumber}";
}
