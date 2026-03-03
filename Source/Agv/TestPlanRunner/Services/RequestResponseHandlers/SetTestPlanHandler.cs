using System.Text.Json;
using FluentValidation;
using Gebhardt.Acc.TestPlanRunner.Configuration;
using Gebhardt.Acc.TestPlanRunner.Domain;
using Gebhardt.Acc.TestPlanRunner.Messages.Requests;
using Gebhardt.Acc.TestPlanRunner.Messages.Responses;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TestPlanRunner.Services.RequestResponseHandlers;

/// <summary>
/// Handler for setting (creating/updating) test plans.
/// </summary>
[UsedImplicitly]
public class SetTestPlanHandler : IConsumer<SetTestPlan>
{
    private readonly ILogger<SetTestPlanHandler> _logger;
    private readonly IValidator<TestPlan> _validator;
    private readonly TestPlanRunnerConfiguration _configuration;

    private static readonly JsonSerializerOptions _jsonOptions = TestPlanJsonOptions.CreateForPersistence();

    /// <inheritdoc cref="SetTestPlanHandler"/>
    public SetTestPlanHandler(
        ILogger<SetTestPlanHandler> logger,
        IValidator<TestPlan> validator,
        TestPlanRunnerConfiguration configuration)
    {
        _logger = logger;
        _validator = validator;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SetTestPlan> context)
    {
        var testPlan = context.Message.TestPlan;

        _logger.LogInformation("Setting test plan: {TestPlanName}", testPlan.Name);

        await _validator.ValidateAndThrowAsync(testPlan, context.CancellationToken);

        string directoryPath = _configuration.TestPlanDirectory;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, $"{testPlan.Name}.json");

        string json = JsonSerializer.Serialize(testPlan, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, context.CancellationToken);

        _logger.LogInformation("Successfully persisted test plan to {FilePath}", filePath);

        await context.RespondAsync(new TestPlanSet(testPlan.Name));
    }
}