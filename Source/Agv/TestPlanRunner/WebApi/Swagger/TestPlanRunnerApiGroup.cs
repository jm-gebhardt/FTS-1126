using Gebhardt.Acc.Common.WebApi.Swagger;

namespace Gebhardt.Acc.TestPlanRunner.WebApi.Swagger;

/// <summary>
/// Api Group for TestPlanRunner.
/// </summary>
public class TestPlanRunnerApiGroup : ApiGroup
{
    /// <inheritdoc />
    public override string Name => "rest";

    /// <inheritdoc />
    public override string Title => "Test Plan Runner API";

    /// <inheritdoc />
    public override string Version => "v1";
}