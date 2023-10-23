using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

/// <summary>
/// Defines the creator of an <see cref="ActionGroup"/>.
/// </summary>
[PublicAPI]
public enum ActionGroupCreator
{
    /// <summary>
    /// A Warehouse Management System.
    /// </summary>
    Wms,

    /// <summary>
    /// A simulation module.
    /// </summary>
    Simulation,

    /// <summary>
    /// The TransportOrder service.
    /// </summary>
    TransportOrder,

    /// <summary>
    /// The WebApi of the TransportOrder service.
    /// </summary>
    TransportOrderWebApi
}

/// <summary>
/// Provides extension methods for <see cref="HostOrderCreator"/> in the context of the <see cref="ActionGroupCreator"/>.
/// </summary>
public static class HostOrderCreatorExtension
{
    /// <summary>
    /// Converts the <see cref="HostOrderCreator"/> to an <see cref="ActionGroupCreator"/>.
    /// </summary>
    /// <param name="hostOrderCreator"></param>
    /// <returns></returns>
    public static ActionGroupCreator ConvertToActionGroupCreator(this HostOrderCreator hostOrderCreator)
    {
        string creator = hostOrderCreator.ToString();

        if (Enum.TryParse<ActionGroupCreator>(creator, out var actionGroupCreator))
        {
            return actionGroupCreator;
        }
        throw new ArgumentException($"Unknown action group creator '{creator}'.");
    }
}