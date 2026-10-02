using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Api.Dtos;

/// <summary>
/// Provides a paged query result.
/// </summary>
/// <typeparam name="T">The content type of the result.</typeparam>
/// <param name="Items">The result items.</param>
/// <param name="TotalCount">The total item count.</param>
/// <param name="CurrentPage">The current page (0-based).</param>
/// <param name="PageSize">The page size.</param>
[PublicAPI]
public sealed record PagedResult<T>(ICollection<T> Items, int TotalCount, int CurrentPage, int PageSize)
{ 
    /// <summary>
    /// The amount of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1));

    /// <summary>
    /// True, if there is a next page.
    /// </summary>
    public bool HasNextPage => PageSize > 0 && (CurrentPage + 1) * PageSize < TotalCount;

    /// <summary>
    /// True, if there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 0;
}

