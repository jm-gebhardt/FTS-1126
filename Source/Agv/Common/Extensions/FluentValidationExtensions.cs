using FluentValidation;
using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Extensions for FluentValidation
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Adds a reason via WithState to the validation result. Also adds the reason message via WithMessage.
    /// </summary>
    /// <param name="rule">The current rule</param>
    /// <param name="errorMessageTemplate">The template to use for the reason message. The template can contain placeholders for the parameters, e.g. {0}, {1}, etc.</param>
    /// <param name="parameters">The parameters to use for the reason message. The parameters will be used to replace the placeholders in the template.</param>
    /// <returns></returns>
    [PublicAPI]
    public static IRuleBuilderOptions<T, TProperty> WithReason<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        string errorMessageTemplate,
        params Func<T, object?>[] parameters)
    {
        Reason? reason = null;
        return rule
            .WithMessage(o =>
            {
                reason = new Reason(errorMessageTemplate, parameters.Select(d => d(o)?.ToString()).ToArray());
                return reason.ToString();
            })
            .WithState(_ => reason);
    }

    /// <summary>
    /// Adds a reason via WithState to the validation result. Also adds the reason message via WithMessage.
    /// </summary>
    /// <param name="rule">The current rule</param>
    /// <param name="errorMessageTemplateFunction">The template to use for the reason message. The template can contain placeholders for parameters, e.g. {0}, {1}, etc.</param>
    /// <param name="parameters">The parameters to use for the reason message. The parameters will be used to replace the placeholders in the template.</param>
    /// <returns></returns>
    [PublicAPI]
    public static IRuleBuilderOptions<T, TProperty> WithReason<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        Func<T, string> errorMessageTemplateFunction,
        params Func<T, object?>[] parameters)
    {
        Reason? reason = null;
        return rule
            .WithMessage(o =>
            {
                reason = new Reason(errorMessageTemplateFunction(o), parameters.Select(d => d(o)?.ToString()).ToArray());
                return reason.ToString();
            })
            .WithState(_ => reason);
    }
}