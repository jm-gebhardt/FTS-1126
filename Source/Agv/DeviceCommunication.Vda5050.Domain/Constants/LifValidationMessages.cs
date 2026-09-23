using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;

/// <summary>
/// Provides the descriptions reported in lif validation results.
/// </summary>
[PublicAPI]
public static class LifValidationMessages
{
    /// <summary>
    /// Reported when a lif file of the same creator is already stored.
    /// </summary>
    public const string CreatorAlreadyExists = "A lif file with the same creator already exists";
}