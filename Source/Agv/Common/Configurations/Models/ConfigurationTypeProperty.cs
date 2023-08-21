namespace Gebhardt.StoreWare.Agv.Common.Configurations.Models;

/// <summary>
/// The model for the property of a configuration
/// </summary>
/// <param name="Key">The unique key of the property</param>
/// <param name="Value">The value of the property</param>
/// <param name="AutoSeedToDatabase">If true this property get's seeded to the database with it's value</param>
public record ConfigurationTypeProperty(string Key, string? Value, bool AutoSeedToDatabase);