using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;

namespace Gebhardt.Acc.Common.Testing.Helpers;

/// <summary>
/// Helper class to generate AgvError test data
/// </summary>
public static class AgvErrorGenerator
{
    private static readonly AgvPosition _defaultAgvPosition = new(true, 1, 1, 1, 0);

    public static AgvErrorStatusReceived GenerateAgvErrorStatus(int errorItems,
        Guid agvCorrelationId,
        string errorCode = "",
        ErrorLevel errorLevel = ErrorLevel.Fatal,
        ErrorCategory errorCategory = ErrorCategory.General,
        AgvPosition? position = null,
        ErrorFlag errorFlag = ErrorFlag.Agv,
        AgvErrorSource errorSource = AgvErrorSource.Agv,
        string? description = null
    )
    {
        var errors = new List<AgvError>();
        for (int i = 0; i < errorItems; i++)
        {
            errors.Add(new AgvError(
                agvCorrelationId,
                $"error title#{i}",
                description ?? $"error description#{i}",
                errorCode,
                errorLevel,
                $"error type#{i}",
                errorCategory,
                new List<ErrorReference>(),
                DateTime.UtcNow,
                errorFlag,
                errorSource
            ));
        }

        return new AgvErrorStatusReceived(agvCorrelationId, errors, position ?? _defaultAgvPosition);
    }

    public static AgvError GenerateAgvError(Guid? agvCorrelationId = null,
        List<ErrorReference>? errorReferences = null,
        string errorCode = "",
        ErrorLevel errorLevel = ErrorLevel.Fatal,
        ErrorCategory errorCategory = ErrorCategory.General,
        ErrorFlag errorFlag = ErrorFlag.Agv,
        DateTime errorResolved = default,
        Guid agvErrorCorrelationId = default,
        AgvErrorSource errorSource = AgvErrorSource.Agv
    )
    {
        agvCorrelationId ??= Guid.NewGuid();
        var agvError = new AgvError(agvCorrelationId.Value,
            $"ErrorTitle({agvCorrelationId})",
            $"ErrorDescription({agvCorrelationId})",
            errorCode,
            errorLevel,
            $"ErrorType({agvCorrelationId})",
            errorCategory,
            errorReferences ?? [new ErrorReference { ReferenceKey = $"ReferenceKey#{agvCorrelationId}", ReferenceValue = $"ReferenceValue#{agvCorrelationId}" }],
            DateTime.UtcNow,
            errorFlag,
            errorSource
        )
        {
            CorrelationId = agvErrorCorrelationId == Guid.Empty ? Guid.NewGuid() : agvErrorCorrelationId
        };
        if (errorResolved != default)
        {
            agvError.ErrorResolved = errorResolved;
        }
        return agvError;
    }
}