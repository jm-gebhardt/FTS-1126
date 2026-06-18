namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Defines if the action is required. REQUIRED: must always be communicated. CONDITIONAL: may or may not be required. OPTIONAL: may or may not be communicated at the master control system's discretion.
/// </summary>
public enum RequirementType
{
    /// <summary/>
    Conditional,

    /// <summary/>
    Optional,

    /// <summary/>
    Required
}
