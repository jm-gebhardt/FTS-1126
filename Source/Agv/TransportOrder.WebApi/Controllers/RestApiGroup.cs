using Gebhardt.Acc.Common.WebApi.Swagger;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers;

/// <summary>
/// Api Group for REST
/// </summary>
public class RestApiGroup : ApiGroup
{
    /// <summary>
    /// Api group name to be used with ApiExplorerSettings attribute
    /// </summary>
    public const string GroupName = "rest";

    /// <inheritdoc />
    public override string Name => $"{Version}-{GroupName}";

    /// <inheritdoc />
    public override string Title => "REST API";

    /// <inheritdoc />
    public override string Version => "v1";
}