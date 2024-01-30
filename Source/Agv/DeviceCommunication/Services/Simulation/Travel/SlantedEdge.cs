using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Slanted"/>.
/// </summary>
public class SlantedEdge : BaseEdge
{
    private readonly List<DirectEdge> _sections = new();
    private DirectEdge _currentSection;

    /// <inheritdoc cref="SlantedEdge"/>>
    public SlantedEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode) : base(edge, sourceNode, targetNode)
    {
        double angle = (double)edge.VariantParameters.First().Scalar!;
        double distance = (double)edge.VariantParameters.Last().Scalar!;
        double outAngle = 2 * Math.PI + angle; // 
        var virtualNodes = new List<GraphNode>
        {
            new("1") { X = sourceNode.X - (float)(distance * Math.Sin(angle)), Y = sourceNode.Y - (float)(distance * Math.Cos(angle)) },
            new("2") { X = targetNode.X + (float)(distance * Math.Sin(outAngle)), Y = targetNode.Y + (float)(distance * Math.Cos(outAngle)) }
        };
        var source = SourceNode;
        GraphEdge? sectionEdge;
        DirectEdge? section;
        foreach (var virtualNode in virtualNodes)
        {
            sectionEdge = CreateDirect(source.Id, virtualNode.Id, (float)Mathematics.EuclideanDistance(source, virtualNode), Guid.NewGuid());
            section = new DirectEdge(sectionEdge, source, virtualNode);
            _sections.Add(section);
            source = virtualNode;
        }
        sectionEdge = CreateDirect(source.Id, TargetNode.Id, (float)Mathematics.EuclideanDistance(source, TargetNode), Guid.NewGuid());
        section = new DirectEdge(sectionEdge, source, TargetNode);
        _sections.Add(section);
        _currentSection = _sections.First();
        ReferenceTarget = _currentSection.TargetNode;
        AgvOrientation = _sections.Last().AgvOrientation;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void SetAngles(double entryAngle, GraphNode? nextSource, GraphNode? nextTarget)
    {
        EntryAngle = entryAngle;
        foreach (var item in _sections.Select((section, index) => new { section, index }))
        {
            var target = item.index <= _sections.Count - 2 ? _sections[item.index + 1].TargetNode : nextTarget;
            var source = item.index <= _sections.Count - 2 ? _sections[item.index + 1].SourceNode : nextSource;
            item.section.SetAngles(entryAngle, source, target);
            entryAngle = item.section.ExitAngle;
        }
        ExitAngle = _sections.Last().ExitAngle;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void EstimateTravelTime(AgvData agvData, double initialVelocity, DateTime entryTime)
    {
        EntryTime = entryTime;
        _sections.ForEach(s =>
        {
            s.EstimateTravelTime(agvData, initialVelocity, entryTime);
            entryTime = s.EstimatedExitTime;
            initialVelocity = s.ExitVelocity;
        });
        InitialTurnFinishTime = _sections.First().InitialTurnFinishTime;
        _currentSection = _sections.First();
        var lastSection = _sections.Last();
        EstimatedExitTime = lastSection.EstimatedExitTime;
        ExitVelocity = lastSection.ExitVelocity;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override AgvPosition GetCurrentPosition(DateTime now, AgvData agvData, float previousOrientation)
    {
        GetCurrentSection(now);
        return _currentSection.GetCurrentPosition(now, agvData, GetPreviousOrientation(previousOrientation));
    }

    private void GetCurrentSection(DateTime now)
    {
        var section = _sections.SingleOrDefault(s => s.EstimatedExitTime >= now && s.EntryTime <= now);
        _currentSection = section ?? _sections.Last();
    }

    private float GetPreviousOrientation(float initialOrientation)
    {
        int sectionIndex = _sections.IndexOf(_currentSection);
        return sectionIndex == 0 ? initialOrientation : _sections[sectionIndex - 1].AgvOrientation;
    }
}