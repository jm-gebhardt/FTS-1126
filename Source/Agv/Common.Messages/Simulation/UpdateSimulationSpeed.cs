namespace Gebhardt.Acc.Common.Messages.Simulation;

/// <summary>
/// Message indicating a command to update the simulation speed.
/// </summary>
/// <param name="SimulationSpeed">The factor applied to agv velocities and acceleration to control the speed of the simulation.</param>
public record UpdateSimulationSpeed(int SimulationSpeed);