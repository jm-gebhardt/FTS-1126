namespace Gebhardt.Acc.TransportOrder.Domain.Extensions;

/// <summary>
/// Extension methods for AdditionalInfo of stations.
/// </summary>
public static class AdditionalInformationExtensions
{
    /// <summary>
    /// Safely gets a boolean value from the additional info dictionary.
    /// </summary>
    public static bool GetBool(this Dictionary<string, object?>? additionalInfo, string key, bool defaultValue = false)
    {
        bool boolValue = defaultValue;
        if (additionalInfo != null && additionalInfo.TryGetValue(key, out object? value))
        {
            bool.TryParse(value?.ToString(), out bool b);
            boolValue = b;
        }
        return boolValue;
    }
}