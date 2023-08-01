using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Queries;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// The result of <see cref="GetAgvRequest"/>
/// </summary>
/// <param name="Agv"></param>
public record GetAgvResult(IAgv Agv);
