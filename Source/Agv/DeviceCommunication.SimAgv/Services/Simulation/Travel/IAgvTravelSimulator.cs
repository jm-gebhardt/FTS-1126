using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;

/// <summary>
/// Provides methods to simulate the agv travel on edges.
/// </summary>
public interface IAgvTravelSimulator
{
    /// <summary>
    /// The edge the agv is currently traveling on.
    /// </summary>
    BaseEdge? CurrentEdge { get; }

    /// <summary>
    /// The current node of the agv.
    /// </summary>
    GraphNode? CurrentNode { get; }

    /// <summary>
    /// The travel duration.
    /// </summary>
    TimeSpan? TravelDuration { get; }

    /// <summary>
    /// Total time the agv was paused / not driving. Zero if there was no pause.
    /// </summary>
    TimeSpan PauseTime { get; }

    /// <summary>
    /// The current position of the agv.
    /// </summary>
    AgvPosition Position { get; }

    /// <summary>
    /// Estimates travel times on the current edges.
    /// </summary>
    void EstimateTravelTimes(IReadOnlyList<string> nodes, IReadOnlyList<Guid> edges, DateTime entryTime, double initialVelocity);

    /// <summary>
    /// Estimates travel time to target position.
    /// </summary>
    void EstimateTravelTimes(AgvPosition targetPosition, DateTime entryTime, double initialVelocity);

    /// <summary>
    /// Updates the current position.
    /// </summary>
    void UpdatePosition();

    /// <summary>
    /// Immediately stops the agv.
    /// </summary>
    void Pause();

    /// <summary>
    /// Immediately resumes the agv. This accelerates the agv to the previous speed in no time.
    /// </summary>
    void Resume();

    /// <summary>
    /// Finishes edge travel.
    /// </summary>
    void FinishTravel();

    /// <summary>
    /// Get the shortest edge of the current route.
    /// </summary>
    /// <returns></returns>
    BaseEdge GetShortestEdge();
}