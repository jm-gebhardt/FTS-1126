namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public readonly record struct AgvId(string Manufacturer, string SerialNumber)
{
    public override string ToString() => $"{Manufacturer}/{SerialNumber}";
}
