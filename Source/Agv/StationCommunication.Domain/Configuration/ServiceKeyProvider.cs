namespace Gebhardt.Acc.StationCommunication.Domain.Configuration;

/// <summary>
/// Provides service keys
/// </summary>
public static class ServiceKeyProvider
{
    /// <summary>
    /// Key for services sending telegrams to the PLC that
    /// understands 'AgvCargoState'
    /// </summary>
    public const string AgvCargoState = "AGVCARGOSTATE";

    /// <summary>
    /// Key for services sending telegrams to the PLC that
    /// understands 'AplWeStation'
    /// </summary>
    public const string AplWeStation = "APLWESTATION";

    /// <summary>
    /// Key for services receiving telegrams from the PLC that
    /// sends 'JamState'
    /// </summary>
    public const string FromWeStation = "FROMWESTATION";
}