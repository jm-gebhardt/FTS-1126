using System.Reflection;
using Gebhardt.Acc.Common.Services.Filters;

namespace Gebhardt.Acc.Common.Services;

/// <summary>
/// Uses reflection to check for equals changes in objects. If a custom action is defined for a property, it will be executed instead.
/// </summary>
public abstract class ReflectionSignificanceChecker<T> : ISignificanceChecker<T>
{
    /// <summary>
    /// Custom defined determination of significant changes based on the properties of the object.
    /// </summary>
    /// <returns>The dictionary of property names and the function to determine if the change is significant.</returns>
    protected abstract Dictionary<string, Func<T, T, bool>> GetSignificantChangeByPropertyName();

    /// <inheritdoc />
    public virtual bool HasSignificantChanges(T obj1, T obj2)
    {
        if (obj1 == null && obj2 == null)
        {
            return false;
        }
        if (obj1 == null || obj2 == null)
        {
            return true;
        }

        Dictionary<string, Func<T, T, bool>> actionByPropertyName = GetSignificantChangeByPropertyName();

        PropertyInfo[] properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            object? currentValue = property.GetValue(obj1);
            object? previousValue = property.GetValue(obj2);

            if (currentValue != null &&
                previousValue != null &&
                actionByPropertyName.TryGetValue(property.Name, out Func<T, T, bool>? action))
            {
                bool hasSignificantChanges = action(obj1, obj2);
                if (hasSignificantChanges)
                {
                    return hasSignificantChanges;
                }
                continue;
            }

            if (!Equals(currentValue, previousValue))
            {
                return true;
            }
        }
        return false;
    }
}