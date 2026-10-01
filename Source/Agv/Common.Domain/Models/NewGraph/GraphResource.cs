namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a resource in the graph.
/// </summary>
public abstract record GraphResource
{
    /// <summary>
    /// Unique identifier for the graph resource.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Name of the graph resource.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Short name of the graph resource.
    /// </summary>
    public string ShortName { get; init; }

    /// <summary>
    /// Identifier of the graph segment this resource was added or last updated with.
    /// </summary>
    public Guid? GraphSegmentId { get; set; }

    /// <inheritdoc cref="GraphResource"/>
    protected GraphResource(Guid id, string name, string shortName)
    {
        Id = id;
        Name = name;
        ShortName = shortName;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    protected GraphResource()
    {
        Name = null!;
        ShortName = null!;
    }
}