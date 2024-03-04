using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// Represents the categories of <see cref="AgvError"/>.
/// </summary>
[PublicAPI]
public enum ErrorCategory
{
    /// <summary/>
    General,

    /// <summary/>
    Dirt,

    /// <summary/>
    Temperature
}