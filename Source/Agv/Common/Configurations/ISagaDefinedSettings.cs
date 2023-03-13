namespace Gebhardt.StoreWare.Agv.Common.Configurations
{
    /// <summary>
    /// Implement this interface in a Saga to get it read by <see ref="SagaSettingsProvider"/>
    /// </summary>
    /// <typeparam name="TSagaSettings">Settings class saved in the saga, which has nullable properties</typeparam>
    /// <typeparam name="TSettings">The final settings class to be used everywhere</typeparam>
    public interface ISagaDefinedSetting<out TSagaSettings, TSettings>
    where TSagaSettings : ISagaSettings<TSettings>
    where TSettings : class, ISettings<TSettings>
    {
        /// <summary>
        /// The saga settings object
        /// </summary>
        /// <returns>The settings in the Saga</returns>
        TSagaSettings? Settings { get; }
    }
}