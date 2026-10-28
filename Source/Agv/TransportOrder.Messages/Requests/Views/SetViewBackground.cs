using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>Represents a command to store or replace the background image of a view. Setting the background of a generated view stores it.</summary>
/// <param name="CorrelationId">The id of the view.</param>
/// <param name="Resolution">The resolution of the image in [m] per pixel.</param>
/// <param name="XOffset">The x offset in [m] where the graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset in [m] where the graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="Image">The PNG image bytes.</param>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record SetViewBackground(Guid CorrelationId, double Resolution, double XOffset, double YOffset, byte[] Image) : CorrelatedBy<Guid>;