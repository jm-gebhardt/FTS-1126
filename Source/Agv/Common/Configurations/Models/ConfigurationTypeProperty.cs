namespace Gebhardt.Acc.Common.Configurations.Models;

/// <summary>
/// The model for the property of a configuration
/// </summary>
/// <param name="Key">The unique key of the property</param>
/// <param name="Value">The value of the property</param>
/// <param name="AutoSeedToDatabase">If true this property gets seeded to the database with its value</param>
public record ConfigurationTypeProperty(string Key, string? Value, bool AutoSeedToDatabase);