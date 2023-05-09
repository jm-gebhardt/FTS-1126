using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Extensions
{
    /// <summary>
    /// Extension methods for ConfigurationTypes
    /// </summary>
    public static class ConfigurationTypeExtensions
    {
        /// <summary>
        /// Registers Mock which is returning the given <see cref="AgvSettings"/>
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="settings">The settings to register</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        [UsedImplicitly]
        public static IServiceCollection AddInMemorySettings(this IServiceCollection services, AgvSettings? settings = null)
        {
            var settingsProviderMock = new Mock<ISettingsProvider<AgvSettings>>();
            settingsProviderMock.Setup(m => m.GetSettings()).Returns(() => settings ?? new AgvSettings());
            services.AddSingleton(settingsProviderMock.Object);

            return services;
        }
    }
}