using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TriggerService.Domain.Exceptions;

/// <summary>
/// 
/// </summary>
public class TriggerNotFoundException : NotFoundException
{
    /// <inheritdoc cref="TriggerNotFoundException"/>>
    public TriggerNotFoundException(string paramName) : base(paramName)
    {
    }
}