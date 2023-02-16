using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection for registering settings
    /// </summary>
    public static class ConfigureSettings
    {
        /// <summary>
        /// Registers Mock which is returning the given <see cref="AgvSettings"/>
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="settings">The settings to register. If null then the default value of the settings get used</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        [UsedImplicitly]
        public static IServiceCollection AddAgvSettings(this IServiceCollection services, AgvSettings? settings = null)
        {
            var settingsProviderMock = new Mock<IAgvSettingsProvider>();
            settingsProviderMock.Setup(m => m.GetSettingsForAgv(It.IsAny<Guid>())).Returns(() => settings ?? new AgvSettings());
            services.AddSingleton(settingsProviderMock.Object);

            return services;
        }

        /// <summary>
        /// Registers Mock which is returning the given <see cref="AddSettings{TSettings}"/>
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="settings">The settings to register. If null then the default value of the settings get used</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        [UsedImplicitly]
        public static IServiceCollection AddSettings<TSettings>(this IServiceCollection services, TSettings? settings = null) where TSettings : class, ISettings<TSettings>
        {
            var settingsProviderMock = new Mock<ISettingsProvider<TSettings>>();
            settingsProviderMock.Setup(m => m.GetSettings()).Returns(() => settings ?? Activator.CreateInstance<TSettings>());
            services.AddSingleton(settingsProviderMock.Object);

            return services;
        }
    }
}