using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Polypath"/>.
/// </summary>
public class Polypath : BaseEdge
{
    /// <summary>
    /// The sections in-between the control points.
    /// </summary>
    public List<DirectEdge> Sections { get; } = new();

    private DirectEdge _currentSection;

    /// <inheritdoc cref="Polypath"/>>
    public Polypath(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode) : base(edge, sourceNode, targetNode)
    {
        List<EdgeCoefficient> controlPoints = edge.VariantParameters.ToList();
        var source = SourceNode;
        GraphEdge? sectionEdge;
        DirectEdge? section;
        foreach (var controlPoint in controlPoints.Select((edgeCoefficient, index) => new { edgeCoefficient, index }))
        {
            var target = new GraphNode(controlPoint.index.ToString()) { X = (float)controlPoint.edgeCoefficient.X!, Y = (float)controlPoint.edgeCoefficient.Y! };
            sectionEdge = CreateDirect(source.Id, target.Id, (float)Mathematics.EuclideanDistance(source, target), Guid.NewGuid());
            section = new DirectEdge(sectionEdge, source, target);
            Sections.Add(section);
            source = target;
        }
        sectionEdge = CreateDirect(source.Id, TargetNode.Id, (float)Mathematics.EuclideanDistance(source, TargetNode), Guid.NewGuid());
        section = new DirectEdge(sectionEdge, source, TargetNode);
        Sections.Add(section);
        _currentSection = Sections.First();
        ReferenceTarget = _currentSection.TargetNode;
        AgvOrientation = Sections.Last().AgvOrientation;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void EstimateTravelTime(AgvData agvData, double initialVelocity, DateTime entryTime)
    {
        InitialVelocity = initialVelocity;
        EntryTime = entryTime;
        Sections.ForEach(s =>
        {
            s.EstimateTravelTime(agvData, initialVelocity, entryTime);
            entryTime = s.EstimatedExitTime;
            initialVelocity = s.ExitVelocity;
        });
        InitialTurnFinishTime = Sections.First().InitialTurnFinishTime;
        _currentSection = Sections.First();
        var lastSection = Sections.Last();
        EstimatedExitTime = lastSection.EstimatedExitTime;
        ExitVelocity = lastSection.ExitVelocity;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void SetAngles(double entryAngle, GraphNode? nextSource, GraphNode? nextTarget)
    {
        EntryAngle = entryAngle;
        foreach (var item in Sections.Select((section, index) => new { section, index }))
        {
            var target = item.index <= Sections.Count - 2 ? Sections[item.index + 1].TargetNode : nextTarget;
            var source = item.index <= Sections.Count - 2 ? Sections[item.index + 1].SourceNode : nextSource;
            item.section.SetAngles(entryAngle, source, target);
            entryAngle = item.section.ExitAngle;
        }
        ExitAngle = Sections.Last().ExitAngle;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override AgvPosition GetCurrentPosition(DateTime now, AgvData agvData, float previousOrientation)
    {
        GetCurrentSection(now);
        return _currentSection.GetCurrentPosition(now, agvData, GetPreviousOrientation(previousOrientation));
    }

    private void GetCurrentSection(DateTime now)
    {
        var section = Sections.SingleOrDefault(s => s.EstimatedExitTime >= now && s.EntryTime <= now);
        _currentSection = section ?? Sections.Last();
    }

    private float GetPreviousOrientation(float initialOrientation)
    {
        int sectionIndex = Sections.IndexOf(_currentSection);
        return sectionIndex == 0 ? initialOrientation : Sections[sectionIndex - 1].AgvOrientation;
    }
}