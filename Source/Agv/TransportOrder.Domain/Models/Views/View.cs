using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Views;

/// <summary>
/// A view on the graph. One level, one area and optionally one background image.
/// </summary>
[PublicAPI]
public class View
{
    private const string GeneratedIdPrefix = "View.";
    private const string DefaultName = "Default";
    private const double GeneratedAreaMargin = 1.0;

    /// <summary>
    /// The id of the view.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The name of the view.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The id of the level the view shows.
    /// </summary>
    public string? LevelId { get; private set; }

    /// <summary>
    /// Whether the view is opened first.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// The area of the graph the view shows.
    /// </summary>
    public ViewArea Area { get; private set; }

    /// <summary>
    /// The placement of the background image. Null if the view has none.
    /// </summary>
    public ViewBackground? Background { get; private set; }

    /// <summary>
    /// Whether the view is generated from the graph instead of defined by a user.
    /// </summary>
    public bool IsGenerated { get; private set; }

    /// <inheritdoc cref="View"/>
    public View(Guid id, string name, string? levelId, bool isDefault, ViewArea area, ViewBackground? background = null, bool isGenerated = false)
    {
        Id = id;
        Name = name;
        LevelId = levelId;
        IsDefault = isDefault;
        Area = area;
        Background = background;
        IsGenerated = isGenerated;
    }

    /// <summary>For deserialization.</summary>
    [UsedImplicitly]
    private View()
    {
        Name = null!;
        Area = null!;
    }

    private static View CreateGenerated(string? levelId, ViewArea area, bool isDefault)
    {
        return new View($"{GeneratedIdPrefix}{levelId}".ToGuid(), levelId ?? DefaultName, levelId, isDefault, area.Enlarge(GeneratedAreaMargin), isGenerated: true);
    }

    /// <summary>
    /// Creates one generated view per level, or one empty view when there is no level.
    /// </summary>
    /// <param name="levelAreas">The areas covered by each level.</param>
    /// <returns>The generated views ordered by level. The first one is the default view.</returns>
    public static IReadOnlyList<View> CreateGenerated(IEnumerable<LevelArea> levelAreas)
    {
        var ordered = levelAreas.OrderBy(l => l.LevelId, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0)
        {
            return [CreateGenerated(null, new ViewArea(0, 0, 0, 0), isDefault: true)];
        }

        return ordered.Select((level, index) => CreateGenerated(level.LevelId, level.Area, index == 0)).ToList();
    }

    /// <summary>
    /// Applies the given changes. Null values stay unchanged.
    /// </summary>
    public void Update(string? name, string? levelId, bool? isDefault, ViewArea? area)
    {
        Name = name ?? Name;
        LevelId = levelId ?? LevelId;
        IsDefault = isDefault ?? IsDefault;
        Area = area ?? Area;
        IsGenerated = false;
    }

    /// <summary>
    /// Sets the placement of the background image.
    /// </summary>
    public void SetBackground(ViewBackground background)
    {
        Background = background;
        IsGenerated = false;
    }

    /// <summary>
    /// Removes the background image.
    /// </summary>
    public void RemoveBackground()
    {
        Background = null;
    }

    /// <summary>
    /// Marks the view as not opened first.
    /// </summary>
    public void ClearDefault()
    {
        IsDefault = false;
    }
}