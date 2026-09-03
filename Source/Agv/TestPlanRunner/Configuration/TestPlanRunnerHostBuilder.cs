using System.Text.Json.Serialization;
using FluentValidation;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.Common.WebApi.Swagger;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Domain.Parsing;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TestPlanRunner.Domain.Validators;
using Gebhardt.Acc.TestPlanRunner.Messages.Requests;
using Gebhardt.Acc.TestPlanRunner.Services;
using Gebhardt.Acc.TestPlanRunner.Services.EventHandlers;
using Gebhardt.Acc.TestPlanRunner.Services.RequestResponseHandlers;
using Gebhardt.Acc.TestPlanRunner.WebApi.Middleware;
using Gebhardt.Acc.TestPlanRunner.WebApi.Swagger;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Core.EntityFrameworkCore;
using Gebhardt.Core.Hosting.Configuration;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace Gebhardt.Acc.TestPlanRunner.Configuration;

/// <summary>
/// Contains the host building logic for TestPlanRunner.
/// </summary>
public static class TestPlanRunnerHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="initialLogger">The initial logger</param>
    public static IHostBuilder CreateHostBuilder(string[] args, ILogger initialLogger)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .ConfigureAppConfiguration((context, conf) =>
            {
                conf.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

                // Configuration
                string sectionName = ConfigurationTypeManager.Instance.GetSectionName<TestPlanRunnerConfiguration>();
                var config = context.Configuration.GetSection(sectionName).Get<TestPlanRunnerConfiguration>()
                             ?? new TestPlanRunnerConfiguration();
                services.AddSingleton(config);

                // Database
                services.AddDbContextFactory<TransportOrderDbContext>(builder =>
                {
                    string? connectionString = context.Configuration.GetConnectionString(DbProviders.SqlServer)
                                               ?? context.Configuration.GetConnectionString(TransportOrderDbContext
                                                   .ConfigSectionName);
                    builder.UseSqlServer(connectionString,
                        options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                    builder.AddInterceptors(new EfCommandMetricsInterceptor());
                });

                // Parsers
                services.AddSingleton<JsonTestPlanParser>();
                services.AddSingleton<YamlTestPlanParser>();
                services.AddSingleton<ITestPlanParserFactory, TestPlanParserFactory>();

                // Validators
                services.AddSingleton<IValidator<TestPlan>, TestPlanValidator>();

                // Services
                services.AddSingleton<IStationProvider, StationProvider>();
                services.AddSingleton<IAgvExecutionStateProvider, AgvExecutionStateProvider>();
                services.AddSingleton<ITestPlanDictionaryProvider, TestPlanDictionaryProvider>();
                services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));
                services.AddSingleton(typeof(IConcurrentDictionaryProvider<>), typeof(ConcurrentDictionaryProvider<>));
                services.AddSingleton(typeof(IKeyedSemaphoreProvider<>), typeof(KeyedSemaphoreProvider<>));
                services.AddSingleton<IAgvRepository, AgvRepository>(); // captive (ITestPlanManager is singleton)
                services.AddTransient<IActionGroupRepository, EfActionGroupRepository>();
                services.AddSingleton<IAgvRoutePlanCacheProvider, AgvRoutePlanCache>();
                services.AddSingleton<IAgvRouteRetrievalService, AgvRouteRetrievalService>(); // captive (ITestStepCancellationEvaluator is singleton)
                services.AddSingleton<ITestPlanManager, TestPlanManager>();
                services.AddSingleton<ITestStepCancellationEvaluator, TestStepCancellationEvaluator>();
                services.AddSingleton<TestPlanFileWatcher>();
                services.AddHostedService<PollingBackgroundService<TestPlanFileWatcher>>();

                // WebAPI
                services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });
                services.AddTransient<ExceptionHandlerMiddleware>();

                // MassTransit
                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration, x =>
                    {
                        x.AddConsumer<TestPlanTriggerEventHandler>();

                        x.AddMediator(c =>
                        {
                            c.AddConsumer<GetTestPlansHandler>();
                            c.AddConsumer<SetTestPlanHandler>();
                            c.AddConsumer<DeleteTestPlanHandler>();

                            c.AddRequestClient<GetTestPlans>();
                            c.AddRequestClient<GetTestPlan>();
                            c.AddRequestClient<SetTestPlan>();
                            c.AddRequestClient<DeleteTestPlan>();
                        });
                    });
                }
                catch (InvalidOperationException e)
                {
                    initialLogger.LogError(e, "MassTransit could not be started");
                    throw;
                }
                if (!context.HostingEnvironment.IsEnvironment("SwaggerCli"))
                {
                    services.ConfigureTracing(context.Configuration);
                }

                // Swagger
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc(new TestPlanRunnerApiGroup());
                    options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                    options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                    options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                    options.UseAllOfForInheritance(); // Allows $ref objects to be nullable

                    options.IncludeXmlComments();
                });
            });

        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .ConfigureAppConfiguration((_, config) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                })
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseMiddleware<ExceptionHandlerMiddleware>();
                    app.UseRouting();

                    app.UseSwagger();
                    app.UseSwaggerUI(options => { options.SwaggerEndpoint(new TestPlanRunnerApiGroup()); });

                    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
                });
        });

        return hostBuilder;
    }
}