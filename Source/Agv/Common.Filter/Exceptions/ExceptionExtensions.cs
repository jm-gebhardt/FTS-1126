using System.Runtime.ExceptionServices;

namespace Gebhardt.Acc.Common.Filter.Exceptions;

internal static class ExceptionExtensions
{
    public static Exception GetBaseExceptionKeepingStacktrace(this Exception exception)
    {
        return ExceptionDispatchInfo.Capture(exception.GetBaseException()).SourceException;
    }
}