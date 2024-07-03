using Gebhardt.Acc.Common.TelegramCommunication.Configuration;

namespace Gebhardt.Acc.Common.Testing.TelegramCommunication;

/// <summary>
/// Provides a telegram communication configuration for testing
/// </summary>
public static class TestConfigurationProvider
{
    /// <summary>
    /// Get the configuration
    /// </summary>
    /// <returns></returns>
    public static TelegramCommunicationConfiguration GetConfiguration()
    {
        return new TelegramCommunicationConfiguration("ip", 5000, 1000, true, 2, 180000);
    }
}