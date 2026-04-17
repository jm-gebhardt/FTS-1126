using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.StationCommunication.Configuration;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.StationCommunication.Services;

/// <summary>
/// Determines which PLC channel keys are active based on application configuration.
/// </summary>
[PublicAPI]
public class ActiveChannelKeyProvider(IConfiguration configuration)
{
    private static readonly string[] _allKeys = [ServiceKeyProvider.AplWeStation, ServiceKeyProvider.FromWeStation, ServiceKeyProvider.AgvCargoState];

    /// <summary>
    /// Returns channel keys whose configuration section exists and has UseConnection enabled.
    /// </summary>
    public List<string> GetActiveChannelKeys()
    {
        var active = new List<string>();

        foreach (string key in _allKeys)
        {
            var section = configuration.GetSection(key);
            if (!section.Exists())
            {
                continue;
            }

            var config = new TelegramCommunicationConfiguration();
            config.LoadFromConfig(section);

            if (config.UseConnection)
            {
                active.Add(key);
            }
        }

        return active;
    }
}