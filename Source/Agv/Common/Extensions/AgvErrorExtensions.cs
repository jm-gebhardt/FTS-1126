using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Extensions for <see cref="AgvError"/>.
/// </summary>
public static class AgvErrorExtensions
{
    /// <summary>
    /// Check if the list of errors contains any fatal error.
    /// </summary>
    /// <param name="errors">The error list to check.</param>
    /// <returns>True if there is any fatal error. False if there are only warnings.</returns>
    public static bool HasAnyError(this IEnumerable<AgvError> errors)
    {
        return errors.Any(e => e.ErrorLevel == ErrorLevel.Fatal);
    }
}