using Gebhardt.StoreWare.Agv.Common.Configurations.Attributes;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Models
{
    /// <summary>
    /// Abstract base record for settings
    /// </summary>
    public abstract record SettingsBase<TSettings> : ISettings<TSettings> where TSettings : class
    {
        /// <summary>
        /// The section name (base key) of the setting. In the base implementation the name of the settings type is used, but you can specify any value by overriding it
        /// </summary>
        [AutoSeedToDatabase(false)]
        public virtual string SectionName => typeof(TSettings).Name;
    }
}

