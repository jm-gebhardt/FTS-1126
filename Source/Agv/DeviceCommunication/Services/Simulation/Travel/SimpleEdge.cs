using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Domain.Utilities;
using Gebhardt.StoreWare.Agv.Common.Extensions;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of that is not a composite of <see cref="EdgeType.Direct"/>.
/// </summary>
public class SimpleEdge : BaseEdge
{
    private bool StopForTurnIntoEdge { get; set; }

    /// <summary>
    /// The time the agv finishes to accelerate.
    /// </summary>
    public DateTime AccelerationFinishTime { get; private set; }

    /// <summary>
    /// The time the agv finishes to run at maximum possible velocity.
    /// </summary>
    public DateTime MaxVelocityFinishTime { get; private set; }

    /// <summary>
    /// The maximum possible velocity reached by the agv [m/s].
    /// </summary>
    public double MaxPossibleVelocity { get; private set; }

    /// <summary>
    /// The length of the section on which the agv accelerates [m].
    /// </summary>
    public double AccelerationSection { get; private set; }

    /// <summary>
    /// The length of the section on which the agv drives at the <see cref="MaxPossibleVelocity"/> [m].
    /// </summary>
    public double MaxVelocitySection { get; private set; }

    /// <summary>
    /// The length of the section on which the agv decelerates [m].
    /// </summary>
    public double DecelerationSection { get; private set; }

    /// <inheritdoc cref="SimpleEdge"/>
    protected SimpleEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode, bool agvGoesBackwards = false) : base(edge, sourceNode, targetNode, agvGoesBackwards)
    {
    }

    /// <inheritdoc cref="BaseEdge"/>
    public override void EstimateTravelTime(AgvData agvData, double initialVelocity, DateTime entryTime)
    {
        InitialVelocity = initialVelocity;
        EntryTime = entryTime;
        StopForTurnIntoEdge = EntryAngle >= agvData.TurnOnSpotThreshold && agvData.TurnOnSpotThreshold != 0;
        double time = 0;
        if (StopForTurnIntoEdge)
        {
            time += EntryAngle / agvData.MaxVelocityTurnOnSpot;
        }
        InitialTurnFinishTime = EntryTime.AddSeconds(time);

        double maxVelocity = Math.Min(GetAgvMaxVelocity(agvData), MaxVelocity);

        bool stopForTurnOutOfEdge = ExitAngle >= agvData.TurnOnSpotThreshold && agvData.TurnOnSpotThreshold != 0;
        // At the end of the edge the agvs velocity can not exceed the final maximum velocity
        double finalVelocityMax = ExitAngle > 0 ? agvData.MaxVelocityTurn : maxVelocity;
        finalVelocityMax = stopForTurnOutOfEdge || TargetNode.IsStation() ? 0 : finalVelocityMax;
        ExitVelocity = finalVelocityMax;

        // Distance needed to decelerate from the maximum velocity to the final velocity
        double maxDecelerationSection = Math.Max(PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, maxVelocity, ExitVelocity), 0);
        // Distance needed to accelerate from the initial velocity to the maximum velocity
        double maxAccelerationSection = Math.Max(PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, maxVelocity, InitialVelocity), 0);

        // Minimum distance needed to decelerate
        double minDecelerationSection = Math.Max(PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, InitialVelocity, ExitVelocity), 0);

        double accelerationTime;
        double decelerationTime;
        double maxVelocityTime;

        // The edge is too short to do anything else but decelerate 
        if (ExitVelocity < InitialVelocity && minDecelerationSection >= Length)
        {
            DecelerationSection = Math.Min(minDecelerationSection, Length);
            AccelerationSection = MaxVelocitySection = 0;
            AccelerationFinishTime = MaxVelocityFinishTime = InitialTurnFinishTime;
            MaxPossibleVelocity = InitialVelocity;
            accelerationTime = maxVelocityTime = 0;
            decelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, DecelerationSection, InitialVelocity);
            time += accelerationTime + maxVelocityTime + decelerationTime;
            EstimatedExitTime = EntryTime.AddSeconds(time);
            return;
        }

        // The edge is too short to accelerate to maximum and then decelerate 
        if (maxDecelerationSection + maxAccelerationSection > Length)
        {
            if (ExitVelocity > InitialVelocity)
            {
                // accelerate to final velocity than move at final velocity
                ExitVelocity = Math.Min(ExitVelocity, PhysicsHelper.VelocityAfterDistanceFromVelocity(agvData.Acceleration, Length, InitialVelocity));
                MaxPossibleVelocity = ExitVelocity;
                AccelerationSection = Math.Min(PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, MaxPossibleVelocity, InitialVelocity), Length);
                DecelerationSection = 0;
                MaxVelocitySection = Math.Max(Length - AccelerationSection, 0);

                accelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, AccelerationSection, InitialVelocity);
                maxVelocityTime = MaxVelocitySection / MaxPossibleVelocity;
                decelerationTime = 0;
                AccelerationFinishTime = InitialTurnFinishTime.AddSeconds(accelerationTime);
                MaxVelocityFinishTime = AccelerationFinishTime.AddSeconds(maxVelocityTime);
                time += accelerationTime + maxVelocityTime + decelerationTime;
                EstimatedExitTime = EntryTime.AddSeconds(time);
                return;
            }

            if (Math.Abs(ExitVelocity - InitialVelocity) <= double.Epsilon)
            {
                // accelerate on half of the edge, then decelerate
                AccelerationSection = Length / 2d;
                MaxPossibleVelocity = PhysicsHelper.VelocityAfterDistanceFromVelocity(agvData.Acceleration, AccelerationSection, InitialVelocity);
                AccelerationSection = PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, MaxPossibleVelocity, InitialVelocity);
                DecelerationSection = Math.Max(Length - AccelerationSection, 0);
                MaxVelocitySection = 0;

                accelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, AccelerationSection, InitialVelocity);
                decelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, DecelerationSection, ExitVelocity);
                maxVelocityTime = 0;

                AccelerationFinishTime = MaxVelocityFinishTime = InitialTurnFinishTime.AddSeconds(accelerationTime);
                time += accelerationTime + maxVelocityTime + decelerationTime;
                EstimatedExitTime = EntryTime.AddSeconds(time);
                return;
            }

            // move at constant velocity, then decelerate
            DecelerationSection = minDecelerationSection;
            AccelerationSection = 0;
            MaxVelocitySection = Math.Max(Length - DecelerationSection, 0);
            AccelerationFinishTime = InitialTurnFinishTime;
            MaxPossibleVelocity = InitialVelocity;
            accelerationTime = 0;
            decelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, DecelerationSection, InitialVelocity);
            maxVelocityTime = MaxVelocitySection / MaxPossibleVelocity;
            MaxVelocityFinishTime = AccelerationFinishTime.AddSeconds(maxVelocityTime);
            time += accelerationTime + maxVelocityTime + decelerationTime;
            EstimatedExitTime = EntryTime.AddSeconds(time);
            return;
        }

        // The edge is long enough to accelerate to maximum and then decelerate or even drive at maximum velocity in between acceleration and deceleration
        AccelerationSection = maxAccelerationSection;
        MaxVelocitySection = Math.Max(Length - maxDecelerationSection - maxAccelerationSection, 0);
        DecelerationSection = maxDecelerationSection;
        MaxPossibleVelocity = maxVelocity;

        accelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, maxAccelerationSection, InitialVelocity);
        AccelerationFinishTime = InitialTurnFinishTime.AddSeconds(accelerationTime);
        maxVelocityTime = MaxVelocitySection / MaxPossibleVelocity;
        MaxVelocityFinishTime = AccelerationFinishTime.AddSeconds(maxVelocityTime);
        decelerationTime = PhysicsHelper.TimeToAccelerateFromVelocity(agvData.Acceleration, DecelerationSection, finalVelocityMax);
        time += accelerationTime + maxVelocityTime + decelerationTime;
        EstimatedExitTime = EntryTime.AddSeconds(time);
    }
    
    /// <summary>
    /// Gets the maximum velocity of the agv on the edge.
    /// </summary>
    /// <param name="agvData">The agv data.</param>
    /// <returns>The maximum velocity [m/s].</returns>
    protected virtual double GetAgvMaxVelocity(AgvData agvData)
    {
        return agvData.MaxVelocityStraight;
    }

    /// <inheritdoc cref="BaseEdge"/>
    public override AgvPosition GetCurrentPosition(DateTime now, AgvData agvData, float previousOrientation)
    {
       return new AgvPosition(true, 0, 0, 0,  0);
    }

    /// <summary>
    /// Get the agv edge travel distance from the agv data and the current time.
    /// </summary>
    /// <param name="agvData">The agv data.</param>
    /// <param name="now">The current time.</param>
    /// <returns></returns>
    public double GetEdgeTravelDistance(AgvData agvData, DateTime now)
    {
        double distance = 0;
        double secondsOnSection;
        // The agv is stopping for an initial turn.
        if (now <= InitialTurnFinishTime)
        {
            return distance;
        }
        // The agv is accelerating.
        double currentVelocity;
        if (now <= AccelerationFinishTime && AccelerationSection > double.Epsilon)
        {
            secondsOnSection = (now - InitialTurnFinishTime).TotalSeconds;
            currentVelocity = InitialVelocity + agvData.Acceleration * secondsOnSection;
            distance += PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, currentVelocity, InitialVelocity);
            return distance;
        }
        distance += AccelerationSection;
        // The agv is running at maximum possible velocity.
        if (now <= MaxVelocityFinishTime && MaxVelocitySection > double.Epsilon)
        {
            secondsOnSection = (now - AccelerationFinishTime).TotalSeconds;
            distance += GetLinearDistance(secondsOnSection, (MaxVelocityFinishTime - AccelerationFinishTime).TotalSeconds, MaxVelocitySection);
            return distance;
        }
        distance += MaxVelocitySection;
        // The agv is decelerating.
        secondsOnSection = (now - MaxVelocityFinishTime).TotalSeconds;
        currentVelocity = Math.Max(MaxPossibleVelocity - agvData.Acceleration * secondsOnSection, 0);
        double currentDecelerationSection = PhysicsHelper.DistanceToVelocity(agvData.Acceleration, currentVelocity);
        double fullDecelerationSection = PhysicsHelper.DistanceToVelocity(agvData.Acceleration, MaxPossibleVelocity);
        distance += fullDecelerationSection - currentDecelerationSection;
        return distance;
    }

    /// <summary>
        /// Get distance from motion at constant velocity.
        /// </summary>
        /// <param name="secondOnSection">Seconds spend on this section.</param>
        /// <param name="sectionTravelTime">Total travel time [s] on this section.</param>
        /// <param name="sectionLength">Length of the section [m].</param>
        private static double GetLinearDistance(double secondOnSection, double sectionTravelTime, double sectionLength)
        {
            return secondOnSection / sectionTravelTime * sectionLength;
        }
}