using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos.Lif;

/// <summary>
/// Represents a validation result.
/// </summary>
/// <param name="Type">The type of the validated resource.</param>
/// <param name="ResourceId">The id of the validated resource (if applicable).</param>
/// <param name="Severity">The severity of the result.</param>
/// <param name="Description">The result description-</param>
public record LifValidationResultDto(ResourceType Type, string? ResourceId, Severity Severity, string Description);