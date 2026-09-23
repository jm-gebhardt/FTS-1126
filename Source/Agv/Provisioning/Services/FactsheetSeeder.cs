using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;
using Gebhardt.Acc.Provisioning.Configuration;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class FactsheetSeeder : JsonFolderSeeder
{
    private readonly IRequestClient<AddOrUpdateFactsheet> _client;

    /// <inheritdoc />
    protected override string FolderName => "Factsheets";

    /// <inheritdoc cref="FactsheetSeeder"/>
    public FactsheetSeeder(IOptions<ConfigOptions> configOptions, IRequestClient<AddOrUpdateFactsheet> client, ILogger<FactsheetSeeder> logger)
        : base(configOptions.Value.BasePath, logger)
    {
        _client = client;
    }

    /// <inheritdoc />
    protected override async Task SeedFileAsync(string filePath)
    {
        string rawJson = await File.ReadAllTextAsync(filePath);
        var factsheet = DeserializeJson<Factsheet>(rawJson, filePath);
        var correlationId = factsheet.SerialNumber.ToGuid();

        try
        {
            await _client.GetResponse<Success>(new AddOrUpdateFactsheet(correlationId, factsheet, rawJson));
        }
        catch (RequestFaultException exception) when (exception.Fault?.Exceptions.Any(info => info.ExceptionType == typeof(AgvTypeAlreadyExistsException).FullName) == true)
        {
            Logger.LogInformation("Agv type {Manufacturer}.{SeriesName} already exists, skipping", factsheet.Manufacturer, factsheet.TypeSpecification.SeriesName);
            return;
        }

        Logger.LogInformation("Seeded factsheet of agv {AgvCorrelationId}/{SerialNumber}", correlationId, factsheet.SerialNumber);
    }
}