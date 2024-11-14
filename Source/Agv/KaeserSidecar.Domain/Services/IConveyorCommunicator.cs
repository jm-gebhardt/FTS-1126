namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <summary>
/// Provides a service for communicating with the conveyors
/// </summary>
public interface IConveyorCommunicator
{
    /// <summary>
    /// Report the cargo state to the APL conveyors.
    /// </summary>
    /// <param name="cargo"></param>
    void ReportAplCargoState(bool cargo);
    
    /// <summary>
    /// Report the station for pick or drop to the conveyor.
    /// </summary>
    void ReportPalletPickDropStation(string targetStation, string? pickStation, string? dropStation);
}
