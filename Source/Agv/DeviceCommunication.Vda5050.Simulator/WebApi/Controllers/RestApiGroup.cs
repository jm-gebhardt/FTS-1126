using Gebhardt.Acc.Common.WebApi.Swagger;
using JetBrains.Annotations;

namespace DeviceCommunication.Vda5050.Simulator.WebApi.Controllers;

/// <summary>
/// Api Group for REST
/// </summary>
[PublicAPI]
public class RestApiGroup : ApiGroup
{
    /// <summary>
    /// Api group name to be used with ApiExplorerSettings attribute
    /// </summary>
    public const string GroupName = "rest";

    /// <inheritdoc />
    public override string Name => GroupName;

    /// <inheritdoc />
    public override string Title => "REST API";

    /// <inheritdoc />
    public override string Version => "v1";
}
