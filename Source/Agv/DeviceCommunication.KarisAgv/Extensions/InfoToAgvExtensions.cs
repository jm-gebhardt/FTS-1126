using System.Globalization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;

/// <summary>
/// Provides extension methods for InfoToAgvKey dictionary in <see cref="AgvAction"/>.
/// </summary>
public static class InfoToAgvExtensions
{
    private const double RotationValue = Math.PI;

    /// <summary>
    /// Check if value for CargoRotation in InfoToAgv requests rotation.
    /// </summary>
    /// <param name="infoToAgv">Information passed to the agv.</param>
    /// <returns>True if the agv needs to rotate.</returns>
    public static bool NeedsRotation(this Dictionary<InfoToAgvKey, string> infoToAgv)
    {
        infoToAgv.TryGetValue(InfoToAgvKey.CargoRotation, out string? rotationValue);

        if (string.IsNullOrEmpty(rotationValue))
        {
            return false;
        }

        double parsedRotationValue = double.Parse(rotationValue, CultureInfo.InvariantCulture);

        if (Math.Abs(parsedRotationValue - RotationValue) < Mathematics.DoubleComparator)
        {
            return true;
        }

        if (Math.Abs(parsedRotationValue) > RotationValue * 2)
        {
            throw new ArgumentOutOfRangeException(nameof(InfoToAgvKey.CargoRotation), "Value out of range.");
        }

        return false;
    }

    /// <summary>
    /// Returns lift height value in [m] from InfoToAgv dictionary, if available.
    /// </summary>
    /// <param name="infoToAgv"></param>
    /// <returns></returns>
    public static double? GetLiftHeight(this Dictionary<InfoToAgvKey, string> infoToAgv)
    {
        double? liftHeight = null;

        infoToAgv.TryGetValue(InfoToAgvKey.LiftHeight, out string? liftHeightValue);

        if (!liftHeightValue.IsNullOrEmpty())
        {
            if (double.TryParse(liftHeightValue, CultureInfo.InvariantCulture, out double parsedLiftHeight))
            {
                liftHeight = parsedLiftHeight;
            }
        }

        return liftHeight;
    }

    /// <summary>
    /// Returns dock front (bool) from the InfoToAgv dictionary.
    /// </summary>
    public static bool GetDockFront(this Dictionary<InfoToAgvKey, string> infoToAgv)
    {
        bool dockFront = true;
        infoToAgv.TryGetValue(InfoToAgvKey.DockFront, out string? dockFrontValue);
        if (!string.IsNullOrEmpty(dockFrontValue))
        {
            if (bool.TryParse(dockFrontValue, out bool parsedDockFront))
            {
                dockFront = parsedDockFront;
            }
        }
        return dockFront;
    }
}