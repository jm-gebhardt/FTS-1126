using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.CustomerConfiguration;

/// <summary>
/// Provides the customer configuration
/// </summary>
[PublicAPI]
public class CustomerConfiguration
{
    /// <summary>
    /// True, if the configuration has been loaded from file.
    /// </summary>
    public bool IsLoaded { get; set; }

#pragma warning disable CS1591
    public const int ErrorCodeSourceUnknown = 1000;
    public const int ErrorCodeTargetUnknown = 2000;
    public const int ErrorCodeNoRoute = 3000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private int[] KnownValidationErrors => [ErrorCodeSourceUnknown, ErrorCodeTargetUnknown, ErrorCodeNoRoute];

    /// <summary>
    /// Errors for host order validation sent back to Prismat
    /// </summary>
    public Dictionary<int, string> OrderValidationErrors { get; private set; } =
        new()
        {
            { ErrorCodeSourceUnknown, "Quell-Station nicht bekannt" },
            { ErrorCodeTargetUnknown, "Ziel-Station nicht bekannt" },
            { ErrorCodeNoRoute, "Route von Quelle zu Ziel nicht bekannt" }
        };

    /// <inheritdoc cref="CustomerConfiguration"/>>
    public CustomerConfiguration()
    {
        IsLoaded = false;
    }

    /// <summary>
    /// Update the configuration from a yaml file.
    /// </summary>
    /// <param name="configPath"></param>
    public void UpdateFromFile(string configPath)
    {
        using var reader = new StreamReader(configPath);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yamlData = deserializer.Deserialize<CustomerConfiguration>(reader);

        if (!yamlData.OrderValidationErrors.IsNullOrEmpty())
        {
            UpdateOrderValidationErrors(yamlData.OrderValidationErrors);
        }
        IsLoaded = true;
    }

    /// <summary>
    /// Update order validation errors.
    /// </summary>
    /// <param name="updatedValidationErrors"></param>
    public void UpdateOrderValidationErrors(Dictionary<int, string> updatedValidationErrors)
    {
        foreach ((int errorCode, string errorInformation) in updatedValidationErrors)
        {
            if (IsKnownErrorCode(errorCode))
            {
                OrderValidationErrors[errorCode] = errorInformation;
            }
        }
    }

    private bool IsKnownErrorCode(int errorCode)
    {
        if (!KnownValidationErrors.Contains(errorCode))
        {
            throw new Exception($"Unknown order validation error code {errorCode}, must be on of {string.Join(',', KnownValidationErrors.Select(e => e.ToString()))}");
        }
        return true;
    }

    /// <summary>
    /// Get error code and description for given validation error.
    /// </summary>
    /// <param name="validationError">The validation error.</param>
    public (int Code, string Description) GetError(ValidationError validationError)
    {
        switch (validationError)
        {
            case ValidationError.SourceUnknown:
                return (ErrorCodeSourceUnknown, OrderValidationErrors[ErrorCodeSourceUnknown]);

            case ValidationError.TargetUnknown:
                return (ErrorCodeTargetUnknown, OrderValidationErrors[ErrorCodeTargetUnknown]);

            case ValidationError.NoRoute:
                return (ErrorCodeNoRoute, OrderValidationErrors[ErrorCodeNoRoute]);

            default:
                throw new ArgumentOutOfRangeException(nameof(validationError), validationError, null);
        }
    }
}