using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TestPlanRunner.Configuration;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Domain.Parsing;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Watches a directory for test plan file changes.
/// </summary>
[UsedImplicitly]
public class TestPlanFileWatcher : IPollable
{
    private readonly ILogger<TestPlanFileWatcher> _logger;
    private readonly ITestPlanParserFactory _parserFactory;
    private readonly ITestPlanManager _testPlanManager;
    private readonly TestPlanRunnerConfiguration _configuration;

    private readonly Dictionary<string, DateTime> _fileLastWriteTimes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly IBus _bus;
    private readonly IStationProvider _stationProvider;

    /// <inheritdoc />
    public int PollingInterval => _configuration.PollingIntervalMs;

    /// <inheritdoc cref="TestPlanFileWatcher"/>
    public TestPlanFileWatcher(
        ILogger<TestPlanFileWatcher> logger,
        ITestPlanParserFactory parserFactory,
        ITestPlanManager testPlanManager,
        TestPlanRunnerConfiguration configuration,
        IBus bus,
        IStationProvider stationProvider)
    {
        _logger = logger;
        _parserFactory = parserFactory;
        _testPlanManager = testPlanManager;
        _configuration = configuration;
        _bus = bus;
        _stationProvider = stationProvider;
    }

    private static IEnumerable<string> GetSupportedFiles(string directoryPath, IEnumerable<string> extensions)
    {
        foreach (string extension in extensions)
        {
            foreach (string file in Directory.EnumerateFiles(directoryPath, "*" + extension, SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc />
    public async Task Poll(CancellationToken cancellationToken = default)
    {
        string directoryPath = _configuration.TestPlanDirectory;

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogError("Test plan directory does not exist: {DirectoryPath}", directoryPath);
            return;
        }

        try
        {
            await ProcessDirectoryAsync(directoryPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test plan directory: {DirectoryPath}", directoryPath);
        }
    }

    private async Task ProcessDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
    {
        HashSet<string> currentFiles = GetSupportedFiles(directoryPath, _parserFactory.SupportedExtensions).ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> deletedFiles = _knownFiles.Where(f => !currentFiles.Contains(f)).ToList();
        foreach (string deletedFile in deletedFiles)
        {
            await ProcessDeletedFileAsync(deletedFile);
        }

        foreach (string filePath in currentFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessFileAsync(filePath, cancellationToken);
        }

        _knownFiles.Clear();
        foreach (string file in currentFiles)
        {
            _knownFiles.Add(file);
        }
    }

    private async Task UpdateStations(CancellationToken cancellationToken)
    {
        IRequestClient<GetStationLabels>? requestClient = _bus.CreateRequestClient<GetStationLabels>(timeout: TimeSpan.FromSeconds(30));
        Response<GotStationLabels> response = await requestClient.GetResponse<GotStationLabels>(new GetStationLabels(), cancellationToken);
        _stationProvider.Update(response.Message.Labels);

        IRequestClient<GetStationTypes>? stationsClient = _bus.CreateRequestClient<GetStationTypes>(timeout: TimeSpan.FromSeconds(30));
        Response<GotStationTypes> stationsResponse = await stationsClient.GetResponse<GotStationTypes>(new GetStationTypes(), cancellationToken);
        _stationProvider.UpdateStationTypes(stationsResponse.Message.Stations);
    }

    private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsFileReady(filePath))
            {
                _logger.LogWarning("File is locked or being written, skipping: {FilePath}", filePath);
                return;
            }

            var lastWriteTime = File.GetLastWriteTimeUtc(filePath);

            if (_fileLastWriteTimes.TryGetValue(filePath, out var previousWriteTime) && previousWriteTime >= lastWriteTime)
            {
                // File did not change
                return;
            }

            await UpdateStations(cancellationToken);

            var testPlan = await _parserFactory.TryParseAsync(filePath, cancellationToken);
            if (testPlan == null)
            {
                return;
            }

            _fileLastWriteTimes[filePath] = lastWriteTime;

            bool isNew = !_fileLastWriteTimes.ContainsKey(filePath) || previousWriteTime == default;

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (isNew)
            {
                _logger.LogInformation("Test plan file detected: {FilePath}, Name: {TestPlanName}", filePath, testPlan.Name);
            }
            else
            {
                _logger.LogInformation("Test plan file modified: {FilePath}, Name: {TestPlanName}", filePath, testPlan.Name);
            }

            await _testPlanManager.ApplyTestPlanAsync(testPlan);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "File is locked, skipping: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test plan file: {FilePath}", filePath);
        }
    }

    private async Task ProcessDeletedFileAsync(string filePath)
    {
        _logger.LogInformation("Test plan file deleted: {FilePath}", filePath);

        string testPlanName = Path.GetFileNameWithoutExtension(filePath);

        List<TestPlan> activeTestPlans = _testPlanManager.GetActiveTestPlans().ToList();
        var matchingPlan = activeTestPlans.FirstOrDefault(p => p.Name == testPlanName);

        if (matchingPlan != null)
        {
            await _testPlanManager.RemoveTestPlanAsync(matchingPlan.Name);
        }

        _fileLastWriteTimes.Remove(filePath);
    }

    private static bool IsFileReady(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}