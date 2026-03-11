using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Errors;

/// <summary>
/// Provides a result for an operation.
/// </summary>
[PublicAPI]
public record Result
{
    /// <summary>
    /// True, if the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// The error, if the operation was not successful.
    /// </summary>
    public Error? Error { get; }

    /// <inheritdoc cref="Result"/>
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary/>
    public static Result Success() => new(true, null);

    /// <summary/>
    public static Result Failure(Error error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary/>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Provides a result for an operation.
/// </summary>
[PublicAPI]
public record Result<T> : Result
{
    /// <summary>
    /// The resulting value.
    /// </summary>
    public T? Value { get; }

    /// <inheritdoc cref="Result"/>
    private Result(T value) : base(true, null) => Value = value;
   
    private Result(Error error) : base(false, error) { }

    /// <inheritdoc cref="Result"/>
    public static implicit operator Result<T>(T value) => new(value);

    /// <inheritdoc cref="Result"/>
    public static implicit operator Result<T>(Error error) => new(error);
}