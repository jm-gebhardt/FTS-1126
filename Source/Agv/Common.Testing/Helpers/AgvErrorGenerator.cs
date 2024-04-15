using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

/// <summary>
/// Helper class to generate AgvError test data
/// </summary>
public static class AgvErrorGenerator
{
    public static AgvErrorStatusReceived GenerateAgvErrorStatus(int errorItems, Guid agvCorrelationId, string errorCode = "", ErrorLevel errorLevel = ErrorLevel.Fatal, ErrorCategory errorCategory = ErrorCategory.General)
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
                DateTime.UtcNow
            ));
        }

        return new AgvErrorStatusReceived(agvCorrelationId, errors);
    }

    public static AgvError GenerateAgvError(Guid? agvCorrelationId = null, List<ErrorReference>? errorReferences = null, string errorCode = "", ErrorLevel errorLevel = ErrorLevel.Fatal, ErrorCategory errorCategory = ErrorCategory.General)
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
            DateTime.UtcNow);
    }
}