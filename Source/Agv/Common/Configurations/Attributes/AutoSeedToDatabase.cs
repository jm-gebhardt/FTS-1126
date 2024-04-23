namespace Gebhardt.Acc.Common.Configurations.Attributes;

/// <summary>
/// Attribute to specify if this class or property will be automatically seeded to the default database.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class AutoSeedToDatabase : Attribute
{
    /// <summary>
    /// Should it be automatically seeded to the default database?
    /// </summary>
    public bool AutoSeedToDatabaseValue { get; }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="autoSeedToDatabaseValue">Whether this class or property should be automatically seeded to the database as default value</param>
    public AutoSeedToDatabase(bool autoSeedToDatabaseValue)
    {
        AutoSeedToDatabaseValue = autoSeedToDatabaseValue;
    }
}