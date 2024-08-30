using System.Globalization;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;

/// <summary>
/// Provides extension methods for InfoToAgvKey dictionary in <see cref="AgvAction"/>.
/// </summary>
public static class InfoToAgvExtensions
{
    private const string NoRotation = "4";
    private static readonly string[] _allowedValues = [ "", "1", "2", "3", "4" ];

    /// <summary>
    /// Check if value for CargoRotation in InfoToAgv requests rotation.
    /// </summary>
    /// <param name="infoToAgv">Information passed to the agv.</param>
    /// <returns>True if the agv needs to rotate.</returns>
    public static bool NeedsRotation(this Dictionary<InfoToAgvKey, string> infoToAgv)
    {
        bool needsRotation = false;

        infoToAgv.TryGetValue(InfoToAgvKey.CargoRotation, out string? rotationValue);

        if (!rotationValue.IsNullOrEmpty())
        {
            if (!_allowedValues.Contains(rotationValue))
            {
                throw new ArgumentOutOfRangeException(nameof(InfoToAgvKey.CargoRotation), "Value out of range.");
            }
            if (!rotationValue!.Equals(NoRotation, StringComparison.OrdinalIgnoreCase))
            { 
                needsRotation = true;
            }
        }

        return needsRotation;
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
}