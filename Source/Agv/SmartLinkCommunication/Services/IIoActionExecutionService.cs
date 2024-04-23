using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <summary>
/// Interface for IoActionExecutionService
/// </summary>
public interface IIoActionExecutionService
{
    /// <summary>
    /// Executes IO action
    /// </summary>
    /// <param name="actionConfig"></param>
    /// <returns></returns>
    Task ExecuteActionAsync(IoActionConfig actionConfig);
}