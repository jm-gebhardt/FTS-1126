using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Slanted"/>.
/// </summary>
public class SlantedEdge : BaseEdge
{
    private readonly List<DirectEdge> _sections = new();
    private DirectEdge _currentSection;

    /// <inheritdoc cref="SlantedEdge"/>>
    public SlantedEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode, bool agvGoesBackwards = false) : base(edge, sourceNode, targetNode)
    {
        (List<GraphEdge> sections, List<GraphNode> nodes) = edge.GetSections(sourceNode, targetNode);
        nodes.AddRange([sourceNode, targetNode]);
        foreach (var section in sections)
        {
            var source = nodes.Single(n => n.Id == section.FromNodeId);
            var target = nodes.Single(n => n.Id == section.ToNodeId);
            var directEdge = new DirectEdge(section, source, target, agvGoesBackwards && _sections.Count == 0);
            _sections.Add(directEdge);
        }
        _currentSection = _sections.First();
        ReferenceTarget = _currentSection.TargetNode;
        AgvOrientation = _sections.Last().AgvOrientation;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void SetAngles(double entryAngle, GraphNode? nextSource, GraphNode? nextTarget, bool turnAgvAtExit = false)
    {
        EntryAngle = entryAngle;
        foreach (var item in _sections.Select((section, index) => new { section, index }))
        {
            var target = item.index <= _sections.Count - 2 ? _sections[item.index + 1].TargetNode : nextTarget;
            var source = item.index <= _sections.Count - 2 ? _sections[item.index + 1].SourceNode : nextSource;
            item.section.SetAngles(entryAngle, source, target, turnAgvAtExit && item.index == 0);
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