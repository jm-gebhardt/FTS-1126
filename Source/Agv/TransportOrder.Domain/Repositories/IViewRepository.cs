using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for the views on the graph and their background images.
/// </summary>
[PublicAPI]
public interface IViewRepository
{
    /// <summary>
    /// Returns all views ordered by name.
    /// </summary>
    /// <returns>The views, empty if there is none.</returns>
    Task<IReadOnlyList<View>> GetViewsAsync();

    /// <summary>
    /// Returns the view with the given id.
    /// </summary>
    /// <param name="id">The id of the view.</param>
    /// <returns>The view. Null if no view has the given id.</returns>
    Task<View?> FindViewAsync(Guid id);

    /// <summary>
    /// Adds the view or updates the one with the same id. A default view clears the default of all others.
    /// A view without background loses its background image.
    /// </summary>
    /// <param name="view">The view to add or update.</param>
    /// <param name="backgroundImage">The new PNG background image. Null keeps the current one.</param>
    Task AddOrUpdateViewAsync(View view, byte[]? backgroundImage = null);

    /// <summary>
    /// Deletes the view with the given id together with its background image.
    /// </summary>
    /// <param name="id">The id of the view.</param>
    Task DeleteViewAsync(Guid id);

    /// <summary>
    /// Returns the PNG background image of the view with the given id.
    /// </summary>
    /// <param name="viewId">The id of the view.</param>
    /// <returns>The image bytes. Null if the view has no background image.</returns>
    Task<byte[]?> GetBackgroundImageAsync(Guid viewId);
}