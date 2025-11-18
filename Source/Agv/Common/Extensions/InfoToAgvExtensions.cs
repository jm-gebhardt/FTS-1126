using System.Globalization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for InfoToAgvKey dictionary in <see cref="AgvAction"/>.
/// </summary>
public static class InfoToAgvExtensions
{
    private const double RotationValue = 1d;

    /// <summary>
    /// Extension block for <see cref="Dictionary{InfoToAgvKey, String}"/>.
    /// </summary>
    extension(Dictionary<InfoToAgvKey, string> infoToAgv)
    {
        /// <summary>
        /// Check if value for CargoRotation in InfoToAgv requests rotation.
        /// </summary>
        /// <returns>True if the agv needs to rotate.</returns>
        public bool NeedsRotation()
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

            if (Math.Abs(parsedRotationValue) > RotationValue + Mathematics.DoubleComparator)
            {
                throw new ArgumentOutOfRangeException(nameof(InfoToAgvKey.CargoRotation), $"Value {parsedRotationValue} out of range.");
            }

            return false;
        }

        /// <summary>
        /// Returns lift height value in [m] from InfoToAgv dictionary, if available.
        /// </summary>
        /// <returns></returns>
        public double? GetLiftHeight()
        {
            double? liftHeight = null;

            infoToAgv.TryGetValue(InfoToAgvKey.LiftHeight, out string? liftHeightValue);

            if (!string.IsNullOrEmpty(liftHeightValue))
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
        public bool GetDockFront()
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

        /// <summary>
        /// Returns force dropping backwards flag (bool) from the InfoToAgv dictionary.
        /// </summary>
        public bool GetForceDroppingBackwards()
        {
            bool forceDroppingBackWards = false;
            infoToAgv.TryGetValue(InfoToAgvKey.ForceDroppingBackwards, out string? forceDroppingBackWardsValue);
            if (!string.IsNullOrEmpty(forceDroppingBackWardsValue))
            {
                if (bool.TryParse(forceDroppingBackWardsValue, out bool parsedDockFront))
                {
                    forceDroppingBackWards = parsedDockFront;
                }
            }
            return forceDroppingBackWards;
        }
    }
}