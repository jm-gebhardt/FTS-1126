namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a resource in the graph.
/// </summary>
public abstract record GraphResource
{
    /// <summary>
    /// Unique identifier for the graph resource.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Name of the graph resource.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Short name of the graph resource.
    /// </summary>
    public string ShortName { get; }
    
    /// <summary>
    /// Collection of child routing resources.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public IReadOnlyCollection<GraphResource> Children { get; } = [];

    /// <inheritdoc cref="GraphResource"/>
    protected GraphResource(Guid id, string name, string shortName)
    {
        Id = id;
        Name = name;
        ShortName = shortName;
    }
}