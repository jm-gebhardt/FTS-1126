namespace Gebhardt.StoreWare.Agv.Common.Configurations
{

    /// <summary>
    /// A marker interface for the <see cref="ISettings{TSettings}"/>
    /// </summary>
    public interface ISettings
    {

    }

    /// <summary>
    /// An interface for implementing a settings class. Properties should be non nullable
    /// </summary>
    /// <typeparam name="TSettings">Marker interface for the settings class</typeparam>
    // ReSharper disable once UnusedTypeParameter
    public interface ISettings<TSettings> : ISettings where TSettings : class
    {
        /// <summary>
        /// The SectionName of the settings. This might be the name of the settings in the common case
        /// </summary>
        public string SectionName { get; }
    }
}