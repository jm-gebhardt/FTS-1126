namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Provides extension methods for additional agv information.
/// </summary>
public static class AdditionalAgvInformationExtension
{
    /// <summary>
    /// Extract the value for a given key and separator.
    /// </summary>
    /// <param name="additionalInformation"></param>
    /// <param name="key">The additional agv information key.</param>
    /// <param name="separator">The additional agv information value.</param>
    /// <returns></returns>
    public static string? ExtractValue(this string? additionalInformation, string key, string separator)
    {
        if (additionalInformation == null)
        {
            return null;
        }

        string[] keyValuePairs = additionalInformation.Split(separator);

        string? value = null;
        foreach (string pair in keyValuePairs)
        {
            string[] parts = pair.Split(':');
            if (parts.Length == 2 && parts[0].Trim().Equals(key, StringComparison.InvariantCultureIgnoreCase))
            {
                value = parts[1];
                break;
            }
        }
        return value;
    }
}