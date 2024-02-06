using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Provisioning.Services;

/// <summary>
/// Factory for creating an <see cref="ISeeder"/> for creating an Agv.
/// </summary>
internal class AgvSeederFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc cref="AgvSeederFactory"/>
    /// <param name="serviceProvider"></param>
    public AgvSeederFactory(
        IServiceProvider serviceProvider
    )
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates an <see cref="ISeeder"/> for creating an Agv.
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public ISeeder Create(string folderPath)
    {
        var agvStatusSeeder = GetService<AgvStatusSeeder>(folderPath);
        var agvGraphSeeder = GetService<AgvGraphSeeder>(folderPath);
        var agvDataSeeder = GetService<AgvDataSeeder>(folderPath);
        string folderName = Path.GetFileName(folderPath);
        var agvSeeder = new AgvSeeder(agvStatusSeeder,
            agvGraphSeeder,
            agvDataSeeder,
            folderName,
            _serviceProvider.GetRequiredService<ILogger<AgvSeeder>>()
        );
        return agvSeeder;
    }

    private T GetService<T>(params object[] parameters)
    {
        var instance = ActivatorUtilities.CreateInstance<T>(_serviceProvider, parameters);

        return instance ?? throw new InvalidOperationException($"No service found for {typeof(T).Name}");
    }
}