using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

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