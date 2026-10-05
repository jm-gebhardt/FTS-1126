using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Polypath"/>.
/// </summary>
public class PolypathEdge : BaseEdge
{
    /// <summary>
    /// The sections in-between the control points.
    /// </summary>
    public List<DirectEdge> Sections { get; } = new();

    private DirectEdge _currentSection;

    /// <inheritdoc cref="PolypathEdge"/>>
    public PolypathEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode, bool agvGoesBackwards = false) : base(edge, sourceNode, targetNode)
    {
        var timeStamp = new List<DateTime> { sourceNode.TimeStamp, targetNode.TimeStamp }.Max();

        var (sections, nodes) = edge.GetSections(sourceNode, targetNode);
        nodes.AddRange([sourceNode, targetNode]);
        foreach (var section in sections)
        {
            var source = nodes.Single(n => n.Id == section.FromNodeId);
            var target = nodes.Single(n => n.Id == section.ToNodeId);
            var directEdge = new DirectEdge(section, source, target, agvGoesBackwards && Sections.Count == 0) { TimeStamp = timeStamp };
            Sections.Add(directEdge);
        }
        _currentSection = Sections.First();
        ReferenceTarget = _currentSection.TargetNode;
        AgvOrientation = Sections.Last().AgvOrientation;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void EstimateTravelTime(AgvData agvData, double initialVelocity, DateTimeOffset entryTime)
    {
        InitialVelocity = initialVelocity;
        EntryTime = entryTime.UtcDateTime;
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
    public override void SetAngles(double entryAngle, GraphNode? nextSource, GraphNode? nextTarget, bool turnAgvAtExit = false)
    {
        EntryAngle = entryAngle;
        foreach (var item in Sections.Select((section, index) => new { section, index }))
        {
            var target = item.index <= Sections.Count - 2 ? Sections[item.index + 1].TargetNode : nextTarget;
            var source = item.index <= Sections.Count - 2 ? Sections[item.index + 1].SourceNode : nextSource;
            item.section.SetAngles(entryAngle, source, target, turnAgvAtExit && item.index == 0);
            entryAngle = item.section.ExitAngle;
        }
        ExitAngle = Sections.Last().ExitAngle;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override AgvPosition GetCurrentPosition(DateTimeOffset now, AgvData agvData, float previousOrientation)
    {
        GetCurrentSection(now);
        return _currentSection.GetCurrentPosition(now, agvData, GetPreviousOrientation(previousOrientation));
    }

    private void GetCurrentSection(DateTimeOffset now)
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