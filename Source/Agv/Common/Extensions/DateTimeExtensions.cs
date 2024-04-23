namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Provides extension methods for <see cref="DateTime"/>.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> to a string in the ISO 8601 format (in UTC+00:00).
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> in (local time) to convert.</param>
    /// <returns>The <see cref="DateTime"/> in the ISO 8601 format.</returns>
    /// <example>2024-04-22T15:00:00+04:00 -> "2024-04-22T11:00:00Z"</example>
    public static string ToUniversalIso8601(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
    }
}