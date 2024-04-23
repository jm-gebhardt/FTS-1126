namespace Gebhardt.Acc.Statistics.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when two grids are incompatible
/// </summary>
public class IncompatibleGridException : Exception
{
    /// <see cref="IncompatibleGridException"/>>
    public IncompatibleGridException() : base("The grids are incompatible (x or y resolutions do not match)")
    {
    }
}