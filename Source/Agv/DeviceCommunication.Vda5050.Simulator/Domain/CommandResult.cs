namespace DeviceCommunication.Vda5050.Simulator.Domain;

/// <summary>
/// Result of a command that was handled by an AGV.
/// </summary>
public sealed record CommandResult(bool Success, string? Error = null)
{
    public static CommandResult Ok { get; } = new(true);

    public static CommandResult Fail(string error) => new(false, error);
}
