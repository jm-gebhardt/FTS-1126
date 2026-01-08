namespace Gebhardt.Acc.StationCommunication.Domain.Services;

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
    void ReportPalletPickDropStation(string targetStation, string? pickStation, string? dropStation, string? hu);

    /// <summary>
    /// Report the drop of a pallet at PS01/PS02/PS03 to the conveyor.
    /// </summary>
    void ReportPalletDrop(string source, string destination, string hu);

    /// <summary>
    /// Request the state of a station at the conveyor.
    /// </summary>
    void RequestStationState(string station);
}
