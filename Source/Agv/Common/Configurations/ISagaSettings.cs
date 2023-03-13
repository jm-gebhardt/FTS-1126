namespace Gebhardt.StoreWare.Agv.Common.Configurations
{

    /// <summary>
    /// A marker interface for the <see cref="ISettings{TSettings}"/>
    /// Should have same properties as the actual TSettings class
    /// But properties defined here must be marked as nullable so that the SagaSettingsProvider can determine whether to use the default values for unset property
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    public interface ISagaSettings<TSettings> where TSettings : ISettings
    {

    }
}