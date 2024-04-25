using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;

namespace Gebhardt.Acc.Common.Testing.Helpers;

/// <summary>
/// Helper class to generate AgvError test data
/// </summary>
public static class AgvErrorGenerator
{
    private static readonly AgvPosition _defaultAgvPosition = new(true, 1, 1, 1, 0);

    public static AgvErrorStatusReceived GenerateAgvErrorStatus(int errorItems, Guid agvCorrelationId, string errorCode = "", ErrorLevel errorLevel = ErrorLevel.Fatal, ErrorCategory errorCategory = ErrorCategory.General, AgvPosition? position = null, ErrorFlag errorFlag = ErrorFlag.Agv)
    {
        var errors = new List<AgvError>();
        for (int i = 0; i < errorItems; i++)
        {
            errors.Add(new AgvError(
                agvCorrelationId,
                $"error title#{i}",
                $"error description#{i}",
                errorCode,
                errorLevel,
                $"error type#{i}",
                errorCategory,
                new List<ErrorReference>(),
                DateTime.UtcNow,
                errorFlag
            ));
        }

        return new AgvErrorStatusReceived(agvCorrelationId, errors, position?? _defaultAgvPosition);
    }

    public static AgvError GenerateAgvError(Guid? agvCorrelationId = null, List<ErrorReference>? errorReferences = null, string errorCode = "", ErrorLevel errorLevel = ErrorLevel.Fatal, ErrorCategory errorCategory = ErrorCategory.General, ErrorFlag errorFlag = ErrorFlag.Agv)
    {
        agvCorrelationId ??= Guid.NewGuid();
        return new AgvError(agvCorrelationId.Value,
            $"ErrorTitle({agvCorrelationId})",
            $"ErrorDescription({agvCorrelationId})",
            errorCode,
            errorLevel,
            $"ErrorType({agvCorrelationId})",
            errorCategory,
            errorReferences ?? [new ErrorReference { ReferenceKey = $"ReferenceKey#{agvCorrelationId}", ReferenceValue = $"ReferenceValue#{agvCorrelationId}" }],
            DateTime.UtcNow, errorFlag);
    }
}