using Gebhardt.Acc.TestPlanRunner.Configuration;
using Gebhardt.Acc.TestPlanRunner.Domain.Exceptions;
using Gebhardt.Acc.TestPlanRunner.Messages.Requests;
using Gebhardt.Acc.TestPlanRunner.Messages.Responses;
using Gebhardt.Acc.TestPlanRunner.Services.Parsing;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TestPlanRunner.Services.RequestResponseHandlers;

/// <summary>
/// Handler for deleting test plans.
/// </summary>
[UsedImplicitly]
public class DeleteTestPlanHandler : IConsumer<DeleteTestPlan>
{
    private readonly ILogger<DeleteTestPlanHandler> _logger;
    private readonly TestPlanRunnerConfiguration _configuration;
    private readonly ITestPlanParserFactory _parserFactory;

    /// <inheritdoc cref="DeleteTestPlanHandler"/>
    public DeleteTestPlanHandler(
        ILogger<DeleteTestPlanHandler> logger,
        TestPlanRunnerConfiguration configuration,
        ITestPlanParserFactory parserFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _parserFactory = parserFactory;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteTestPlan> context)
    {
        string name = context.Message.TestPlanName;
        string directoryPath = _configuration.TestPlanDirectory;

        bool deleted = false;

        foreach (string extension in _parserFactory.SupportedExtensions)
        {
            string filePath = Path.Combine(directoryPath, name + extension);
            if (!File.Exists(filePath))
            {
                continue;
            }

            _logger.LogInformation("Deleting test plan file: {FilePath}", filePath);
            File.Delete(filePath);
            deleted = true;
        }

        if (!deleted)
        {
            _logger.LogWarning("Test plan file not found for deletion: {TestPlanName}", name);
            throw new TestPlanNotFoundException(name);
        }

        await context.RespondAsync(new TestPlanDeleted(name));
    }
}