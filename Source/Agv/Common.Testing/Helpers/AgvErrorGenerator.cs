using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

/// <summary>
/// Helper class to generate AgvError test data
/// </summary>
public static class AgvErrorGenerator
{
    public static AgvErrorStatusReceived GenerateAgvErrorStatus(int errorItems, Guid agvCorrelationId, ErrorLevel errorLevel = ErrorLevel.Fatal)
    {
        var errors = new List<AgvError>();
        for (int i = 0; i < errorItems; i++)
        {
            errors.Add(new AgvError(
                agvCorrelationId,
                $"error description#{i}",
                errorLevel,
                $"error type#{i}",
                new List<ErrorReference>(),
                DateTime.UtcNow
            ));
        }

        return new AgvErrorStatusReceived(agvCorrelationId, errors);
    }

    public static AgvError GenerateAgvError(Guid? agvCorrelationId = null, List<ErrorReference>? errorReferences = null, ErrorLevel errorLevel = ErrorLevel.Fatal)
    {
        agvCorrelationId ??= Guid.NewGuid();
        return new AgvError(agvCorrelationId.Value,
            $"ErrorDescription({agvCorrelationId})",
            errorLevel,
            $"ErrorType({agvCorrelationId})",
            errorReferences ?? new List<ErrorReference>
            {
                new() {ReferenceKey = $"ReferenceKey#{agvCorrelationId}", ReferenceValue = $"ReferenceValue#{agvCorrelationId}"}
            },
            DateTime.UtcNow);
    }
}