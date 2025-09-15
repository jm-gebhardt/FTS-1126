using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Speed;

/// <summary>
/// Message indicating a command to update the simulation speed.
/// </summary>
/// <param name="CorrelationId">The Agv CorrelationId.</param>
/// <param name="SimulationSpeed">The factor applied to agv velocities and acceleration to control the speed of the simulation.</param>
public record SetSimulationSpeedForAgv(Guid CorrelationId, int SimulationSpeed) : CorrelatedBy<Guid>;