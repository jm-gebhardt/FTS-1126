using JetBrains.Annotations;
using Microsoft.OpenApi.Models;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Swagger;

/// <summary>
/// Api Group Interface
/// </summary>
public interface IApiGroup
{
    /// <summary>
    /// Api Info
    /// </summary>
    public OpenApiInfo ApiInfo { get; }

    /// <summary>
    /// Name of the API
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Title of the API in Swagger UI
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Api Version
    /// </summary>
    [PublicAPI]
    public string Version { get; }
}

/// <summary>
/// 
/// </summary>
public abstract class ApiGroup : IApiGroup
{
    /// <inheritdoc />
    public OpenApiInfo ApiInfo =>
        new()
        {
            Version = Version,
            Title = $"DeviceCommunication.SimAgv {Title}"
        };

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Title { get; }

    /// <inheritdoc />
    public abstract string Version { get; }
}