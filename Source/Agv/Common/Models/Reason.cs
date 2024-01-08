using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Models;

/// <summary>
/// The reason contains a template and parameters to create a reason message. In case of an error, the reason message can be used to create a localized error message.
/// </summary>
public class Reason
{
    /// <summary>
    /// The template to use for the reason message.
    /// The template can contain placeholders for parameters, e.g. {0}, {1}, etc.
    /// </summary>
    [UsedImplicitly]
    public string Template { get; private set; }

    /// <summary>
    /// The parameters to use for the reason message.
    /// The parameter values will be used to replace the placeholders in the template.
    /// </summary>
    [UsedImplicitly]
    public string?[] Parameters { get; private set; }

    /// <inheritdoc cref="Reason"/>>
    [UsedImplicitly]
    private Reason()
    {
        Template = null!;
        Parameters = null!;
    }

    /// <inheritdoc cref="Reason"/>
    /// <param name="template">The template to use for the reason message. The template can contain placeholders for parameters, e.g. {0}, {1}, etc.</param>
    /// <param name="parameters">The parameters to use for the reason message. The parameters will be used to replace the placeholders in the template.</param>
    public Reason(string template, params string?[] parameters)
    {
        Template = template;
        Parameters = parameters;
        Validate(template, parameters);
    }

    /// <summary>
    /// Validates the template and parameters.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="parameters"></param>
    /// <exception cref="ArgumentException"></exception>
    private static void Validate(string template, string?[] parameters)
    {
        if (string.IsNullOrEmpty(template))
        {
            throw new ArgumentException("The template must not be null or empty.");
        }

        List<int> placeholders = Regex.Matches(template, @"\{-?\d+\}")
            .Select(match => int.Parse(match.Value.Trim('{', '}')))
            .ToList();

        if (parameters.Length > placeholders.Count)
        {
            throw new ArgumentException("The number of dynamic values must not be greater than the number of placeholders in the template.");
        }

        if (!placeholders.Any())
        {
            return;
        }

        if (placeholders.Min() != 0 ||
            placeholders.Max() >= parameters.Length)
        {
            throw new ArgumentException("The placeholders in the template must be in the range of the parameters.");
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Parameters is {Length: > 0} ? string.Format(Template, Parameters.Cast<object?>().ToArray()) : Template;
    }
}