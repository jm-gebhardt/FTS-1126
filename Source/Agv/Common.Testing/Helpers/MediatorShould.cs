using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Helpers;

/// <summary>
/// Helper class for asserting Mediator request/client exceptions
/// </summary>
public static class MediatorShould
{
    /// <summary>
    /// Asserts that the given type of exception is thrown during the request
    /// </summary>
    /// <param name="actual"></param>
    /// <param name="customMessage"></param>
    /// <typeparam name="TException"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static async Task<TException> ThrowRequestExceptionAsync<TException>(Func<Task> actual, string? customMessage = null) where TException : Exception
    {
        var result = await Should.ThrowAsync<RequestException>(actual, customMessage);
        var baseException = result.GetBaseException().GetBaseException();
        if (baseException is not TException exception)
        {
            throw new Exception($"{typeof(TException).Name} was not thrown. {baseException}");
        }

        exception.ShouldBeOfType<TException>();
        return exception;
    }
}