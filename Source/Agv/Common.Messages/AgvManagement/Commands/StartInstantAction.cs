using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to start and instant action.
/// </summary>
[PublicAPI]
public record StartInstantAction
{
    /// <summary>
    /// The agv serial number.
    /// </summary>
    public string AgvSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// The type of the instant action.
    /// </summary>
    public string AgvInstantActionType { get; set; } = string.Empty;

    /// <summary>
    /// The blocking type to be used.
    /// </summary>
    public ActionBlockingType? BlockingType { get; set; }

    /// <summary>
    /// Parameter values of the instant action. If not defined, the action has no parameters.
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; } = [];
}

/// <summary>
/// The blocking type of the action.
/// </summary>
[PublicAPI]
public enum ActionBlockingType
{
    /// <summary/>
    None,

    /// <summary/>
    Soft,

    /// <summary/>
    Hard
}

/// <summary>
/// Possible data types for instant action parameters.
/// </summary>
[PublicAPI]
public enum InstantActionParameterType
{
    /// <summary/>
    Bool,

    /// <summary/>
    Number,

    /// <summary/>
    Integer,

    /// <summary/>
    Float,

    /// <summary/>
    String,

    /// <summary/>
    Object,

    /// <summary/>
    Array
}