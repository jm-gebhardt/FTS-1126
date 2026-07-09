using FluentValidation;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.TestPlanRunner.Configuration;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Domain.Parsing;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TestPlanRunner.Messages.Requests;
using Gebhardt.Acc.TestPlanRunner.Messages.Responses;
using JetBrains.Annotations;
using MassTransit;
using System.Text.Json;

namespace Gebhardt.Acc.TestPlanRunner.Services.RequestResponseHandlers;

/// <summary>
/// Handler for setting (creating/updating) test plans.
/// </summary>
/// <inheritdoc cref="SetTestPlanHandler"/>
[UsedImplicitly]
public class SetTestPlanHandler(
    ILogger<SetTestPlanHandler> logger,
    IValidator<TestPlan> validator,
    TestPlanRunnerConfiguration configuration,
    IBus bus,
    IStationProvider stationProvider) : IConsumer<SetTestPlan>
{
    private static readonly JsonSerializerOptions _jsonOptions = TestPlanJsonOptions.CreateForPersistence();

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SetTestPlan> context)
    {
        var testPlan = context.Message.TestPlan;

        logger.LogInformation("Setting test plan: {TestPlanName}", testPlan.Name);

        await Validate(testPlan, context.CancellationToken);

        string directoryPath = configuration.TestPlanDirectory;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, $"{testPlan.Name}.json");

        string json = JsonSerializer.Serialize(testPlan, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, context.CancellationToken);

        logger.LogInformation("Successfully persisted test plan to {FilePath}", filePath);

        await context.RespondAsync(new TestPlanSet(testPlan.Name));
    }

    private async Task Validate(TestPlan testPlan, CancellationToken cancellationToken)
    {
        IRequestClient<GetStationLabels>? requestClient = bus.CreateRequestClient<GetStationLabels>(timeout: TimeSpan.FromSeconds(30));
        Response<GotStationLabels> response = await requestClient.GetResponse<GotStationLabels>(new GetStationLabels(), cancellationToken);
        stationProvider.Update(response.Message.Labels);

        IRequestClient<GetStationTypes>? stationsClient = bus.CreateRequestClient<GetStationTypes>(timeout: TimeSpan.FromSeconds(30));
        Response<GotStationTypes> stationsResponse = await stationsClient.GetResponse<GotStationTypes>(new GetStationTypes(), cancellationToken);
        stationProvider.UpdateStationTypes(stationsResponse.Message.Stations);

        await validator.ValidateAndThrowAsync(testPlan, cancellationToken);
    }
}