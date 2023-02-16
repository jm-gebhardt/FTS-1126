using Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders
{

    /// <summary>
    /// Provides the settings from the configuration
    /// </summary>
    /// <typeparam name="TSettings"></typeparam>
    public class SettingsProvider<TSettings> : ISettingsProvider<TSettings> where TSettings : class, ISettings<TSettings>
    {
        private readonly IConfiguration _configuration;
        private readonly string _sectionName = typeof(TSettings).Name;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="configuration"></param>
        public SettingsProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private TSettings? GetSettingsWithoutThrowing()
        {
            var settings = _configuration.GetSection(_sectionName).Get<TSettings>();
            return settings;
        }

        /// <inheritdoc/>
        public TSettings GetSettings()
        {
            var settings = GetSettingsWithoutThrowing();

            if (settings == null)
            {
                throw new NoSettingsFoundException(_sectionName, typeof(TSettings).Name);
            }
            return settings;
        }

        /// <inheritdoc />
        public bool DoTheSettingsExist()
        {
            var settings = GetSettingsWithoutThrowing();
            return settings != null;
        }
    }
}