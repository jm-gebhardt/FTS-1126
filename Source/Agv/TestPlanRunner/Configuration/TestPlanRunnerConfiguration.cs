using Gebhardt.Acc.Common.Configurations.Attributes;
using Gebhardt.Acc.Common.Configurations.Models;

namespace Gebhardt.Acc.TestPlanRunner.Configuration;

/// <summary>
/// Configuration for the TestPlanRunner service.
/// </summary>
[AutoSeedToDatabase(false)]
public record TestPlanRunnerConfiguration : SettingsBase<TestPlanRunnerConfiguration>
{
    /// <inheritdoc/>
    public override string SectionName => "TestPlanRunner";

    /// <summary>
    /// The directory path where test plan files are located.
    /// </summary>
    public string TestPlanDirectory { get; init; } = "Config/Examples/";

    /// <summary>
    /// The polling interval in milliseconds for checking test plan file changes.
    /// </summary>
    public int PollingIntervalMs { get; init; } = 1000;
}
