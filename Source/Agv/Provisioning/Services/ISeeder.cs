namespace Gebhardt.Acc.Provisioning.Services;

/// <summary>
/// Seeds data to the main system.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// The human-readable name of the seeder.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Executes the seeding.
    /// </summary>
    /// <returns></returns>
    Task SeedAsync();
}