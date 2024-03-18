using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.WebApi;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Dtos;

/// <summary>
/// Represents a dto that is returned when an <see cref="AgvError"/> has been generated with fatal level.
/// </summary>
/// <param name="Error">The generated error.</param>
public record ErrorGeneratedDto([PublicAPI]AgvError Error) : DatedMessage;