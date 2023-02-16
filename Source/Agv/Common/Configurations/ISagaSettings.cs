namespace Gebhardt.StoreWare.Agv.Common.Configurations
{
    /// <summary>
    /// Implement this interface in a Saga to get it read by SagaSettingsProvider
    /// </summary>
    public interface ISagaSetting<out TSettings>
    {
        /// <summary>
        /// The settings object
        /// </summary>
        /// <returns>The settings in the Saga</returns>
        TSettings? Settings { get; }
    }
}