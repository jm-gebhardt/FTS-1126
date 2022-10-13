namespace Gebhardt.StoreWare.Agv.Common.Services.Detectors;

/// <summary>
/// Detects whether the object has changed
/// Returns true if the previous object is null
/// Uses Equals method on the object for comparison
/// </summary>
public class ChangeDetector<T> : IChangeDetector<T>
{
    /// <inheritdoc/>
    public bool HasChanged(T? previousObject, T? newObject)
    {
        return !Equals(previousObject, newObject);
    }
}