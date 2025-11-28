namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Provides length limits for storing strings in the database
/// </summary>
public static class DbStringLimits
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const int OrderId = 200;
    public const int Hu = 200;

    // TODO: Check usages and potentially rename to NodeName
    public const int NodeId = 450;
    public const int Description = 400; // descriptive fields (names, types, reasons, specifications..)
    public const int Enum = 100;
    public const int User = 200;
    public const int State = 50;
    public const int ErrorInformation = 50;
    public const int ActionType = 50;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}