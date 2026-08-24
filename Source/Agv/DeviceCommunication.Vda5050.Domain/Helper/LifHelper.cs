using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;

/// <summary>
/// Contains helper methods for Lifs.
/// </summary>
public static class LifHelper
{
    /// <summary>
    /// Deterministically generates a Guid from lif information.
    /// </summary>
    /// <param name="lifId">The id of the lif object.</param>
    /// <param name="layoutId">The id of the layout that this object is in.</param>
    /// <param name="projectIdentification">The project identification from the lif metadata.</param>
    public static Guid GenerateInternalId(string lifId, string layoutId, string projectIdentification)
    {
        return $"{projectIdentification}.{layoutId}.{lifId}".ToGuid();
    }
}