using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Gebhardt.Acc.TransportOrder.WebApi;

/// <summary>
/// A convention to exclude remove Controllers from the application runtime.
/// </summary>
public sealed class ExcludeControllersConvention : IApplicationModelConvention
{
    private readonly Func<ControllerModel, bool> _exclude;

    /// <inheritdoc cref="ExcludeControllersConvention"/>
    public ExcludeControllersConvention(Func<ControllerModel, bool> exclude)
    {
        _exclude = exclude;
    }

    /// <inheritdoc />
    public void Apply(ApplicationModel app)
    {
        for (int i = app.Controllers.Count - 1; i >= 0; i--)
        {
            if (_exclude(app.Controllers[i]))
            {
                app.Controllers.RemoveAt(i);
            }
        }
    }
}