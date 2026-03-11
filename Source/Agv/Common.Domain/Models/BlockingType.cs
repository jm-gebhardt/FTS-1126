using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// The blocking type of the action.
/// </summary>
[PublicAPI]
public enum BlockingType
{
    /// <summary/>
    NONE,

    /// <summary/>
    SOFT,

    /// <summary/>
    HARD
}