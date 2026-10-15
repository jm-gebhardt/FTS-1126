namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public sealed record AgvAction(
    string ActionId,
    string ActionType,
    BlockingType BlockingType,
    string? Description,
    IReadOnlyList<ActionParameter> Parameters);

public enum BlockingType
{
    None,
    Soft,
    Hard
}

public sealed record ActionParameter(string Key, string Value);