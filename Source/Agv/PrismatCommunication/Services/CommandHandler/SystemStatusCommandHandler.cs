using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;

/// <summary>
/// Provides handlers for system status related commands.
/// </summary>
[UsedImplicitly]
public class SystemStatusCommandHandler(ISystemStatusRepository repository, ILogger<SystemStatusCommandHandler> logger, IHostCommunicator communicator) :
    IConsumer<GetSystemStatus>,
    IConsumer<SetReady>,
    IConsumer<SetNotReady>,
    IConsumer<UseAklAgvLimit>
{
    private const string SystemError = "Keine Fahrzeuge vorhanden.";
    private const int SystemErrorCode = 5000;

    /// <summary>
    /// Handles the <see cref="GetSystemStatus"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetSystemStatus> context)
    {
        var result = await GetStatus();
        logger.LogInformation("Reporting system status isReady: {IsReady}, code: {ErrorCode}, info: {ErrorInformation}", result.status.IsReady, result.status.ErrorCode, result.status.ErrorInfo);
        communicator.ReportSystemStatus(result.status.IsReady, result.status.ErrorCode, result.status.ErrorInfo, DateTime.UtcNow);
    }

    /// <summary>
    /// Handles the <see cref="SetReady"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetReady> context)
    {
        var result = await GetStatus(true);

        if (result.status.IsReady && !result.created)
        {
            logger.LogDebug("System already reported as ready");
            return;
        }

        result.status.SetReady();
        await repository.UpdateAsync(result.status);
        await repository.SaveAsync();
        logger.LogInformation("Reporting system status isReady: {IsReady}, code: {ErrorCode}, info: {ErrorInformation}", result.status.IsReady, result.status.ErrorCode, result.status.ErrorInfo);
        communicator.ReportSystemStatus(result.status.IsReady, result.status.ErrorCode, result.status.ErrorInfo, DateTime.UtcNow);
    }

    /// <summary>
    /// Handles the <see cref="SetNotReady"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetNotReady> context)
    {
        var result = await GetStatus();

        if (!result.status.IsReady && !result.created)
        {
            logger.LogDebug("System already reported as not ready");
            return;
        }

        result.status.SetNotReady(SystemErrorCode, SystemError);
        await repository.UpdateAsync(result.status);
        await repository.SaveAsync();
        logger.LogInformation("Reporting system status isReady: {IsReady}, code: {ErrorCode}, info: {ErrorInformation}", result.status.IsReady, result.status.ErrorCode, result.status.ErrorInfo);
        communicator.ReportSystemStatus(result.status.IsReady, result.status.ErrorCode, result.status.ErrorInfo, DateTime.UtcNow);
    }

    private async Task<(SystemStatus status, bool created)> GetStatus(bool isReady = false)
    {
        var systemStatus = await repository.GetStatus();
        if (systemStatus != null)
        {
            return (systemStatus, false);
        }

        systemStatus = new SystemStatus(isReady, isReady ? 0 : SystemErrorCode, isReady ? "" : SystemError);
        logger.LogInformation("No system status found, generating new status.");
        repository.Add(systemStatus);
        await repository.SaveAsync();
        return (systemStatus, true);
    }

    /// <summary>
    /// Handles the <see cref="UseAklAgvLimit"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UseAklAgvLimit> context)
    {
        var (status, _) = await GetStatus();

        status.SetUseAklAgvLimit(context.Message.UseLimit);
        await repository.UpdateAsync(status);
        await repository.SaveAsync();
        logger.LogInformation("Using akl agv limit: {Flag} from now on", status.UseAklAgvLimit);
    }
}