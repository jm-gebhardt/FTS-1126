using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.Common.Services.Filters;

/// <summary>
/// Defines a filter mechanism to evaluate objects and determine their eligibility for further processing based on specific criteria.
/// </summary>
/// <typeparam name="T">The type of object to be evaluated, required to implement IMessageCreatedDate for time-based filtering decisions.</typeparam>
public interface IMessageFilter<in T> where T : IMessageCreatedDate
{
    /// <summary>
    /// Evaluates a given object to decide if it meets the criteria for further processing.
    /// </summary>
    /// <param name="value">The object to be evaluated.</param>
    /// <returns>True if the object meets the defined criteria and should undergo further processing; otherwise, false.</returns>
    Task<bool> ShouldActAsync(T value);
}
