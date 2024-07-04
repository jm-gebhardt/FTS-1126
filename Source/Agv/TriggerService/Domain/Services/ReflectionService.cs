using Microsoft.Extensions.Logging;
using System.Reflection;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TriggerService.Domain.Services;

/// <summary>
/// Provides a service for creating instances of reflected types
/// including dependencies.
/// </summary>
[UsedImplicitly]
public class ReflectionService(IServiceProvider serviceProvider, ILogger<ReflectionService> logger)
{
    /// <summary>
    /// Get all non-abstract implementations of <see cref="ITrigger"/> in given folder.
    /// </summary>
    /// <param name="folder">The folder to look for implementations.</param>
    /// <returns>Collection of types implementing the interface</returns>
    public IEnumerable<Type> GetITriggerImplementations(string folder)
    {
        return GetTriggerAssemblies(logger, folder)
            .SelectMany(s => s.GetTypes())
            .Where(t => t.GetInterface(nameof(ITrigger)) != null
                        && t.IsClass
                        && typeof(ITrigger).IsAssignableFrom(t)
                        && !t.IsAbstract);
    }

    /// <summary>
    /// Loads all trigger assemblies.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="triggerFolder">The folder where trigger dlls are loaded into.</param>
    /// <returns>A collection of assemblies found in the trigger folder.</returns>
    public static ICollection<Assembly> GetTriggerAssemblies(ILogger logger, string triggerFolder)
    {
        var assemblies = new List<Assembly>();
        string current = Path.Join(Directory.GetCurrentDirectory(), triggerFolder);
        if (!Directory.Exists(current))
        {
            logger.LogWarning("No triggers available");
            return [];
        }
        foreach (string dll in Directory.GetFiles(current, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                assemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                logger.LogError("Could not load trigger dll {Message}", ex.Message);
            }
        }
        return assemblies;
    }

    /// <summary>
    /// Get an instance of the given type.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <param name="typeToReflect">The type to reflect from.</param>
    /// <param name="constructorArguments">Constructor argument instances.</param>
    /// <returns></returns>
    public T? GetInstance<T>(Type typeToReflect, object[] constructorArguments)
        where T : class
    {
        string? propertyTypeAssemblyQualifiedName = typeToReflect.AssemblyQualifiedName;
        ConstructorInfo[] constructors = typeToReflect.GetConstructors();
        
        // No public available constructors.
        if (constructors.Length == 0)
        {
            LogConstructorError(typeToReflect, constructorArguments);
            return null;
        }

        // Get constructor with given argument instances,
        ParameterInfo[]? parameters = GetConstructor(constructors, constructorArguments)?.GetParameters();
        if (parameters == null)
        {
            LogConstructorError(typeToReflect, constructorArguments);
            return null;
        }

        // Get constructor argument instances from dependency injection
        object?[] injectedParameters;
        if (constructorArguments.Length == 0)
        {
            // Get all from dependency injection
            injectedParameters = parameters.Select(parameter => serviceProvider.GetService(parameter.ParameterType)).ToArray();
        }
        else
        {
            // Get residual from dependency injection
            injectedParameters = constructorArguments
            .Concat(parameters.Skip(constructorArguments.Length).Select(parameter => serviceProvider.GetService(parameter.ParameterType)))
            .ToArray();
        }

        if (injectedParameters.Contains(null))
        {
            LogDependencyError(injectedParameters, parameters, propertyTypeAssemblyQualifiedName!);
            return null;
        }

        return (T)Activator.CreateInstance(Type.GetType(propertyTypeAssemblyQualifiedName!)!, injectedParameters)!;
    }

    private void LogDependencyError(object?[] injectedParameters, ParameterInfo[]? requiredParameters, string typeName)
    {
        var unknownDependencies = injectedParameters
            .Select((parameter, index) => new { parameter, index })
            .Where(injected => injected.parameter == null);

        foreach (var dependency in unknownDependencies)
        {
            logger.LogError("Could not resolve dependency {Dependency} for {Name}", requiredParameters![dependency.index].ParameterType.Name, typeName);
        }
    }

    /// <summary>
    /// Logs a constructor error
    /// </summary>
    /// <param name="typeToReflect">The type to reflect.</param>
    /// <param name="constructorArguments">The constructor arguments</param>
    private void LogConstructorError(Type typeToReflect, object[] constructorArguments)
    {
        logger.LogError("Failed to create instance of {TypeName}, could not find construction with arguments {ConstructorArguments}", typeToReflect.Name, constructorArguments.Select(item => item.GetType().Name + ","));
    }

    /// <summary>
    /// Takes the arguments of a constructor
    /// </summary>
    /// <param name="constructor">The constructor.</param>
    /// <param name="constructorArgumentsCount">The number of constructor arguments</param>
    /// <returns>ParameterInfo array of all constructor arguments.</returns>
    private ParameterInfo[] TakeArguments(MethodBase constructor, int constructorArgumentsCount)
    {
        ParameterInfo[] parameters = constructor.GetParameters();
        return parameters.Length < constructorArgumentsCount ? parameters : parameters.Take(constructorArgumentsCount).ToArray();
    }

    /// <summary>
    /// Validates the arguments of a constructor
    /// </summary>
    /// <param name="constructor">The constructor.</param>
    /// <param name="constructorArguments">The constructor arguments.</param>
    /// <returns>True, if all constructor arguments could be found.</returns>
    private bool ValidateConstructorArguments(ConstructorInfo constructor, IReadOnlyList<object> constructorArguments)
    {
        if (constructorArguments.Count == 0)
        {
            return true;
        }
        ParameterInfo[] parameters = TakeArguments(constructor, constructorArguments.Count);

        for (int i = 0; i < parameters.Length; i++)
        {
            var requiredParameter = constructorArguments[i].GetType();
            bool isIsAssignableFrom = parameters[i].ParameterType.IsInterface && parameters[i].ParameterType.IsAssignableFrom(requiredParameter);
            if (parameters[i].ParameterType != requiredParameter && !isIsAssignableFrom)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets a constructor.
    /// </summary>
    /// <param name="constructors">The constructors to select from.</param>
    /// <param name="constructorArguments">The constructor arguments</param>
    /// <returns></returns>
    private ConstructorInfo? GetConstructor(IEnumerable<ConstructorInfo> constructors, object[] constructorArguments)
    {
        return constructors.FirstOrDefault(constructor => ValidateConstructorArguments(constructor, constructorArguments));
    }
}