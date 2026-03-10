namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <summary>
/// Exception raised when parsing NURBS into graph edges fails.
/// </summary>
public class NurbsParsingException: Exception
{
    /// <inheritdoc/>
    public NurbsParsingException(string description): base(description){}
}