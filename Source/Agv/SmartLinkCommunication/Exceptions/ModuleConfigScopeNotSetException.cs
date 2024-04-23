using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Exceptions;

/// <summary>
/// Thrown when the <see cref="ModuleConfig"/> is not set in the scope.
/// </summary>
public class ModuleConfigScopeNotSetException : Exception;