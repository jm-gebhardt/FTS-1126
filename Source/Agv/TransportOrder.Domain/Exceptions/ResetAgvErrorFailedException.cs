using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Thrown when resetting agv errors failed.
/// </summary>
public class ResetAgvErrorFailedException : Exception
{
    /// <inheritdoc cref="ResetAgvErrorFailedException"/>
    /// <param name="message"></param>
    /// <param name="stillActiveErrors"></param>
    public ResetAgvErrorFailedException(string message, List<AgvError>? stillActiveErrors) : base($"{message}. {GetErrorString(stillActiveErrors)}")
    {
    }

    private static string GetErrorString(List<AgvError>? stillActiveErrors) => stillActiveErrors != null ? string.Join(", ", stillActiveErrors.Select(error => error.ErrorDescription)) : "";
}