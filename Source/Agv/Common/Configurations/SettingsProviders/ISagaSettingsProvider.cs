using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders
{
    /// <summary>
    /// Provides settings
    /// </summary>
    /// <typeparam name="TSettings">The settings type</typeparam>
    /// <typeparam name="TSaga">The type of the saga in which the setting is stored</typeparam>
    // ReSharper disable once UnusedTypeParameter
    [UsedImplicitly]
    // ReSharper disable once UnusedTypeParameter
    public interface ISagaSettingsProvider<out TSettings, TSaga>
        where TSettings : class, ISettings<TSettings>
        where TSaga : class, SagaStateMachineInstance

    {
        /// <summary>
        /// Retrieves the specific settings out of the Saga
        /// </summary>
        /// <returns>The settings from the top most ConfigurationProvider</returns>
        public TSettings GetSettings(Guid correlationId);
    }
}