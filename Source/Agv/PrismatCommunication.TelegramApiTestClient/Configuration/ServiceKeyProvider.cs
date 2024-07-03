namespace Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Configuration;

/// <summary>
/// Provides service keys
/// </summary>
public static class ServiceKeyProvider
{
    /// <summary>
    /// Key for services receiving telegrams from the Fts
    /// </summary>
    public const string FromFts = "USTMFR";

    /// <summary>
    /// Key for services sending telegrams to the Fts
    /// </summary>
    public const string ToFts = "MFRUST";
}