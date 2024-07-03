namespace Gebhardt.Acc.PrismatCommunication.TelegramApi;

/// <summary>
/// Provides service keys
/// </summary>
public static class ServiceKeyProvider
{
    /// <summary>
    /// Key for services sending telegrams to prismat
    /// </summary>
    public const string ToPrismat = "USTMFR";

    /// <summary>
    /// Key for services receiving telegrams from prismat
    /// </summary>
    public const string FromPrismat = "MFRUST";
}