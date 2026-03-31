using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;
using Vdma.Lif;
using DomainNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Node;
using DomainTrajectory = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Trajectory;
using DomainControlPoint = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.ControlPoint;
using DomainOrientationType = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.OrientationType;
using DomainRequirementType = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.RequirementType;
using CommonBlockingType = Gebhardt.Acc.Common.Domain.Models.BlockingType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for converting LIF and domain graph models.
/// </summary>
[PublicAPI]
public static class LifMappingExtensions
{
    /// <summary>
    /// Collects all distinct vehicle type Ids from a LIF layout collection.
    /// </summary>
    public static HashSet<string> GetVehicleTypeIds(this LifLayoutCollection lif)
    {
        var vehicleTypeIds = new HashSet<string>();

        foreach (var layout in lif.Layouts)
        {
            foreach (var node in layout.Nodes)
            {
                foreach (var property in node.VehicleTypeNodeProperties)
                {
                    vehicleTypeIds.Add(property.VehicleTypeId);
                }
            }

            foreach (var edge in layout.Edges)
            {
                foreach (var property in edge.VehicleTypeEdgeProperties)
                {
                    vehicleTypeIds.Add(property.VehicleTypeId);
                }
            }
        }

        return vehicleTypeIds;
    }

    /// <summary>
    /// Converts a domain <see cref="DomainNode"/> to a <see cref="GraphNode"/>.
    /// </summary>
    public static GraphNode ToCommonGraphNode(this DomainNode domainNode, DateTime timeStamp)
    {
        return new GraphNode(id: domainNode.Id, name: domainNode.NodeId)
        {
            X = (float)domainNode.Position.X,
            Y = (float)domainNode.Position.Y,
            Theta = domainNode.Theta,
            Z = 0,
            TimeStamp = timeStamp
        };
    }

    /// <summary>
    /// Converts a domain <see cref="DomainTrajectory"/> to <see cref="Nurbs"/>.
    /// </summary>
    public static Nurbs? ToNurbs(this DomainTrajectory? domainTrajectory)
    {
        if (domainTrajectory == null || domainTrajectory.ControlPoints.Count == 0)
        {
            return null;
        }

        return new Nurbs
        {
            Degree = domainTrajectory.Degree,
            KnotVector = domainTrajectory.KnotVector.Select(k => (double)k).ToArray(),
            ControlPoints = domainTrajectory.ControlPoints.Select(cp => new NurbsControlPoint(cp.X, cp.Y, cp.Weight)).ToArray()
        };
    }

    /// <summary>
    /// Converts a <see cref="Trajectory"/> to a <see cref="DomainTrajectory"/>.
    /// </summary>
    public static DomainTrajectory ToDomainTrajectory(this Trajectory? lifTrajectory)
    {
        if (lifTrajectory == null)
        {
            return new DomainTrajectory();
        }

        return new DomainTrajectory
        {
            Degree = (int)(lifTrajectory.Degree ?? 3),
            KnotVector = lifTrajectory.KnotVector.Select(k => (float)k).ToList(),
            ControlPoints = lifTrajectory.ControlPoints.Select(cp => new DomainControlPoint
            {
                X = cp.X,
                Y = cp.Y,
                Weight = cp.Weight
            }).ToList()
        };
    }

    /// <summary>
    /// Maps a <see cref="BlockingType"/> to a <see cref="CommonBlockingType"/>.
    /// </summary>
    public static CommonBlockingType ToCommonBlockingType(this BlockingType blockingType)
    {
        return blockingType switch
        {
            BlockingType.Hard => CommonBlockingType.HARD,
            BlockingType.Soft => CommonBlockingType.SOFT,
            _ => CommonBlockingType.NONE
        };
    }

    /// <summary>
    /// Maps a <see cref="RequirementType"/> to a <see cref="DomainRequirementType"/>.
    /// </summary>
    public static DomainRequirementType? ToDomainRequirementType(this RequirementType? requirementType)
    {
        return requirementType switch
        {
            RequirementType.Required => DomainRequirementType.Required,
            RequirementType.Optional => DomainRequirementType.Optional,
            RequirementType.Conditional => DomainRequirementType.Conditional,
            _ => null
        };
    }

    /// <summary>
    /// Maps an orientation type string to a domain orientation type.
    /// </summary>
    public static DomainOrientationType ToDomainOrientationType(this string? orientationType)
    {
        return orientationType?.ToUpperInvariant() switch
        {
            "TANGENTIAL" => DomainOrientationType.Tangential,
            _ => DomainOrientationType.Global
        };
    }

    /// <summary>
    /// Converts LIF node action parameters to domain order action parameters.
    /// </summary>
    public static ICollection<OrderActionParameter> ToDomainParameters(this FluffyActionParameter[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return [];
        }

        return parameters.Select(p => new OrderActionParameter { Key = p.Key, Value = p.Value }).ToList();
    }

    /// <summary>
    /// Converts LIF edge action parameters to domain order action parameters.
    /// </summary>
    public static ICollection<OrderActionParameter> ToDomainParameters(this PurpleActionParameter[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return [];
        }

        return parameters.Select(p => new OrderActionParameter { Key = p.Key, Value = p.Value }).ToList();
    }
}