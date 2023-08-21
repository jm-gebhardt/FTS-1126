namespace Gebhardt.StoreWare.Agv.Common.Services.Detectors;

/// <summary>
/// Detects whether an object has changed
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IChangeDetector<in T>
{
    /// <summary>
    /// <inheritdoc cref="IChangeDetector{T}"/>
    /// </summary>
    /// <param name="previousObject">The previous object</param>
    /// <param name="newObject">The new object</param>
    /// <returns>True if the object has changed</returns>
    bool HasChanged(T? previousObject, T? newObject);
}