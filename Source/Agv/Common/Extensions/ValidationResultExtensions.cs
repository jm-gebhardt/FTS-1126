using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Models;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Extensions for the <see cref="ValidationResult"/> class.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Converts the <see cref="ValidationResult"/> to a list of <see cref="Reason"/>.
    /// </summary>
    /// <param name="validationResult"></param>
    /// <returns>An empty list if there are no errors. Otherwise a list of the Reasons</returns>
    public static IEnumerable<Reason> ToReasons(this ValidationResult validationResult)
    {
        foreach (var validationFailure in validationResult.Errors)
        {
            if (validationFailure.CustomState is Reason reason)
            {
                yield return reason;
            }
        }
    }
}