namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

/// <summary>
/// A load.
/// </summary>
/// <param name="Id">The load id.</param>
/// <param name="PickStartTime">The time the pick action started.</param>
/// <param name="LoadType">The type of the load.</param>
/// <param name="LoadPosition">Indicates which load handling/carrying unit of the AGV is used. Optional for vehicles with only one loadPosition.</param>
public record Load(string Id, DateTime PickStartTime, string? LoadType = null, string? LoadPosition = null);