namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Charge station is not available.
/// </summary>
public class ChargeStationNotAvailableException(string stationId) : NodeNotAvailableException(stationId);