using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.WebApi;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Dtos;

/// <summary>
/// Represents a dto that is returned when an <see cref="AgvError"/> has been generated with warning level.
/// </summary>
/// <param name="Warning">The generated warning.</param>
public record WarningGeneratedDto([PublicAPI]AgvError Warning) : DatedMessage;
