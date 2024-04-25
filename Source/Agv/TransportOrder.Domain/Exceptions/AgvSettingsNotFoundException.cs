using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Agv settings not found
/// </summary>
public class AgvSettingsNotFoundException : NotFoundException
{
    /// <inheritdoc cref="AgvSettingsNotFoundException"/>>
    public AgvSettingsNotFoundException(Guid group) : base($"Settings for agv group {group} not found"){}

    /// <inheritdoc cref="AgvSettingsNotFoundException"/>>
    public AgvSettingsNotFoundException() : base("No agv settings group found") { }
}