using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Domain.Models;

/// <summary>
/// warning: AGV is ready to drive without human intervention
/// fatal: AGV is not in running condition.
/// </summary>
[PublicAPI]
public enum ErrorLevel
{
    /// <summary/>
    Fatal,

    /// <summary/>
    Warning
}