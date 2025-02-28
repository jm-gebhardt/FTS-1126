namespace Gebhardt.Acc.KaeserSidecar.Domain.Configuration;

/// <summary>
/// Provides service keys
/// </summary>
public static class ServiceKeyProvider
{
    /// <summary>
    /// Key for services sending telegrams to the PLC that
    /// understands 'AgvCargoState'
    /// </summary>
    public const string AgvCargoState = "AGVCAROSTATE";

    /// <summary>
    /// Key for services sending telegrams to the PLC that
    /// understands 'AplWeStation'
    /// </summary>
    public const string AplWeStation = "APLWESTATION";
}