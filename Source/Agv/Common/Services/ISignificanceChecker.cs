namespace Gebhardt.StoreWare.Agv.Common.Services;

/// <summary>
/// Checks if a property has changed significantly based on a set of rules.
/// </summary>
public interface ISignificanceChecker<in T>
{
    /// <inheritdoc cref="ISignificanceChecker{T}"/>
    /// <param name="obj1">Object 1.</param>
    /// <param name="obj2">Object 2.</param>
    /// <returns>True if the difference between the two objects is significant.</returns>
    bool HasSignificantChanges(T obj1, T obj2);
}