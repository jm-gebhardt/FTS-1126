using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Testing.Time;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Provides a ServiceCollection and Provider.
/// </summary>
public class FixtureBase
{
    private readonly IServiceCollection _serviceCollection = new ServiceCollection();
    private readonly List<Task> _initTasks = [];
    private bool _useAutoMapper;

    /// <summary>
    /// Service provider
    /// </summary>
    public ServiceProvider Provider { get; private set; } = null!;

    [PublicAPI]
    public FakeTimeProvider TimeProvider { get; } = new();

    protected virtual IEnumerable<Profile> GetProfiles() => [];

    public IMapper Mapper { get; private set; } = null!;

    public Mock<IMapper> MapperMock { get; } = new();

    protected FixtureBase()
    {
        LoadAutoMapperProfiles();
    }

    private void LoadAutoMapperProfiles()
    {
        Profile[] profiles = GetProfiles().ToArray();

        _useAutoMapper = profiles.Length != 0;
        if (_useAutoMapper)
        {
            Mapper = MappingHelper.GetMapper(profiles);
        }
    }

    protected virtual void AddAutoMapper(IServiceCollection serviceCollection)
    {
        LoadAutoMapperProfiles();
        if (_useAutoMapper)
        {
            serviceCollection.AddTransient(_ => Mapper);
            serviceCollection.AddTransient<IMapper>(_ => Mapper);
        }
        else
        {
            serviceCollection.AddTransient(_ => MapperMock.Object);
        }
    }

    protected void AddInitTask(Task task)
    {
        _initTasks.Add(task);
    }

    /// <summary>
    /// Adds the service to the service collection before building the service provider
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    [PublicAPI]
    protected virtual FixtureBase AddServices(IServiceCollection collection)
    {
        return this;
    }

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="services">The action of the service to add</param>
    protected void AddServices(params Action<IServiceCollection>[] services)
    {
        foreach (Action<IServiceCollection> service in services)
        {
            service.Invoke(_serviceCollection);
        }
    }

    /// <summary>
    /// Builds the service provider based on the service collection, in order to be able to register services lazy
    /// </summary>
    protected virtual void Build()
    {
        AddServices(s =>
        {
            s.AddSingleton<TimeProvider>(_ => TimeProvider);
            AddAutoMapper(s);
        });
        AddServices(_serviceCollection);

        Provider = _serviceCollection.BuildServiceProvider(true);
    }

    protected async Task BuildAsync()
    {
        await Task.WhenAll(_initTasks);
    }
}