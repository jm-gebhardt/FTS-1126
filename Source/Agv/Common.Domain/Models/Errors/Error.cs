
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Errors;

/// <summary>
/// Provides an error.
/// </summary>
/// <param name="Description">The error description.</param>
public record Error([UsedImplicitly] string Description);
