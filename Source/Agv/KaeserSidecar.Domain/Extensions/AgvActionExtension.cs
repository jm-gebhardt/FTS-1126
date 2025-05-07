using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Extensions;

/// <summary>
/// Provides extension methods for agv actions.
/// </summary>
public static class AgvActionExtension
{
    /// <summary>
    /// Gets the SAP information from the additional action information.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>The value of SAP.</returns>
    public static string? GetSap(this AgvAction action)
    {
        return action.AdditionalInfo.ExtractValue("SAP", ";");
    }
}