using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

/// <summary>
/// Creates a <see cref="IServiceScope"/> scope with it's corresponding <see cref="ModuleConfig"/> registered.
/// </summary>
public interface IModuleScopeFactory
{
    /// <inheritdoc cref="IModuleScopeFactory"/>
    /// <param name="moduleConfig"></param>
    /// <returns>The <see cref="IServiceScope"/> created.</returns>
    IServiceScope CreateScope(ModuleConfig moduleConfig);
}