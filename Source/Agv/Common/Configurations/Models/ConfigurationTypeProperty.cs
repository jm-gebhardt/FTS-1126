namespace Gebhardt.StoreWare.Agv.Common.Configurations.Models
{
    /// <summary>
    /// The model for the property of a configuration
    /// </summary>
    /// <param name="Key">The unique key of the property</param>
    /// <param name="Value">The value of the property</param>
    public record ConfigurationTypeProperty(string Key, string? Value);
}
