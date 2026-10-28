using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Views;

/// <summary>
/// The PNG background image of a view.
/// </summary>
/// <param name="ViewId">The id of the view.</param>
/// <param name="Image">The PNG image bytes.</param>
[PublicAPI]
public record ViewBackgroundImage(Guid ViewId, byte[] Image);