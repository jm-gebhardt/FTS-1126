using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;

internal class NodeHelper 
{
    public Node Node { get; }

    public List<EdgeHelper> IncomingEdges { get; } = [];

    public List<EdgeHelper> OutgoingEdges { get; } = [];

    public IReadOnlyCollection<string> VehicleTypeIds { get; }

    public NodeHelper(Node node)
    {
        Node = node;
        VehicleTypeIds = Node.VehicleTypeNodeProperties.Select(p => p.VehicleTypeId).Distinct().ToList();
    }

    public List<double> GetThetas(string vehicleTypeId)
    {
        List<double> thetas = GetNodeThetas(vehicleTypeId);

        if (GetRotationAtNodeAllowed(vehicleTypeId) == RotationAtNodeAllowed.None || thetas.Count == 0)
        {
            thetas.AddRange(GetIncomingEdgeThetas(vehicleTypeId));
        }

        if (thetas.Count == 0)
        {
            thetas.AddRange(GetOutgoingEdgeThetas(vehicleTypeId));
        }

        // deduplication (round to avoid floating point noise causing near-duplicate angles)
        return thetas.DistinctBy(t => Math.Round(t, 3)).ToList();
    }

    private List<double> GetNodeThetas(string vehicleTypeId)
    {
        return Node.VehicleTypeNodeProperties.Where(p => p.VehicleTypeId == vehicleTypeId).Select(p => p.Theta).Where(t => t.HasValue).Select(t => t!.Value).ToList();
    }

    private List<double> GetIncomingEdgeThetas(string vehicleTypeId)
    {
        IEnumerable<double?> thetas = IncomingEdges.SelectMany(eh => eh.EdgePropertyHelpers).Where(ph => ph.EdgeProperty.VehicleTypeId == vehicleTypeId).Select(ph => ph.EdgeEndTheta);

        return thetas.Where(t => t.HasValue).Select(t => t!.Value).ToList();
    }

    private List<double> GetOutgoingEdgeThetas(string vehicleTypeId)
    {
        IEnumerable<double?> thetas = OutgoingEdges.SelectMany(eh => eh.EdgePropertyHelpers).Where(ph => ph.EdgeProperty.VehicleTypeId == vehicleTypeId).Select(ph => ph.EdgeStartTheta);

        return thetas.Where(t => t.HasValue).Select(t => t!.Value).ToList();
    }

    public RotationAtNodeAllowed GetRotationAtNodeAllowed(string vehicleTypeId)
    {
        List<VehicleTypeEdgeProperty> incomingEdgeProperties = IncomingEdges.SelectMany(eh => eh.EdgePropertyHelpers).Select(ph => ph.EdgeProperty).Where(p => p.VehicleTypeId == vehicleTypeId).ToList();
        List<VehicleTypeEdgeProperty> outgoingEdgeProperties = OutgoingEdges.SelectMany(eh => eh.EdgePropertyHelpers).Select(ph => ph.EdgeProperty).Where(p => p.VehicleTypeId == vehicleTypeId).ToList();

        IEnumerable<string?> rotationAtNodeAllowedStrings = incomingEdgeProperties.Select(p => p.RotationAtEndNodeAllowed).Concat(outgoingEdgeProperties.Select(p => p.RotationAtStartNodeAllowed));

        bool isCwAllowed = true;
        bool isCcwAllowed = true;

        foreach (string? rotationAtNodeAllowedString in rotationAtNodeAllowedStrings)
        {
            if (rotationAtNodeAllowedString is null)
            {
                continue;
            }

            if (string.Equals(rotationAtNodeAllowedString, "CW", StringComparison.OrdinalIgnoreCase))
            {
                isCcwAllowed = false;
            }

            if (string.Equals(rotationAtNodeAllowedString, "CCW", StringComparison.OrdinalIgnoreCase))
            {
                isCwAllowed = false;
            }

            if (string.Equals(rotationAtNodeAllowedString, "NONE", StringComparison.OrdinalIgnoreCase))
            {
                isCwAllowed = false;
                isCcwAllowed = false;
            }
        }

        if (isCwAllowed && isCcwAllowed)
        {
            return RotationAtNodeAllowed.Both;
        }

        if (isCwAllowed)
        {
            return RotationAtNodeAllowed.Cw;
        }

        if (isCcwAllowed)
        {
            return RotationAtNodeAllowed.Ccw;
        }

        return RotationAtNodeAllowed.None;
    }

    private const double PositionTolerance = 1e-3;

    public bool NodePositionEquals(NodeHelper otherNodeHelper)
    {
        double dx = Node.NodePosition.X - otherNodeHelper.Node.NodePosition.X;
        double dy = Node.NodePosition.Y - otherNodeHelper.Node.NodePosition.Y;
        return Math.Sqrt(dx * dx + dy * dy) < PositionTolerance;
    }
}