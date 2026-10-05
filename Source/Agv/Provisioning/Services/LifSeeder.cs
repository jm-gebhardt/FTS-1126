using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using Gebhardt.Acc.Provisioning.Configuration;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class LifSeeder : JsonFolderSeeder
{
    private const string UploadedBy = "provisioning";

    private readonly IRequestClient<UploadLif> _client;
    private readonly TimeProvider _timeProvider;

    /// <inheritdoc />
    protected override string FolderName => "Lif";

    /// <inheritdoc cref="LifSeeder"/>
    public LifSeeder(IOptions<ConfigOptions> configOptions, IRequestClient<UploadLif> client, TimeProvider timeProvider, ILogger<LifSeeder> logger)
        : base(configOptions.Value.BasePath, logger)
    {
        _client = client;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    protected override async Task SeedFileAsync(string filePath)
    {
        byte[] content = await File.ReadAllBytesAsync(filePath);
        string fileName = Path.GetFileName(filePath);
        var request = new UploadLif(Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime, content, fileName, Vda5050Specifications.LifUploadFileType, UploadedBy);

        var response = await _client.GetResponse<LifUploaded, LifValidationFailed>(request);
        if (response.Is(out Response<LifValidationFailed>? failed))
        {
            var results = failed.Message.LifFile.MetaInformation.ValidationResults;
            if (results.Any(result => result.Description == LifValidationMessages.CreatorAlreadyExists))
            {
                Logger.LogInformation("A lif file of the same creator is already stored, skipping {FileName}", fileName);
                return;
            }

            throw new InvalidOperationException($"Validation of lif file {fileName} failed: {string.Join("; ", results.Select(result => result.Description))}");
        }

        Logger.LogInformation("Seeded lif file {FileName}", fileName);
    }
}