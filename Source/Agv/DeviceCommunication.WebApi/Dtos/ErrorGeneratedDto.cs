using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Dtos;

/// <summary>
/// Represents a dto that is returned when an <see cref="AgvError"/> has been generated with fatal level.
/// </summary>
/// <param name="Error">The generated error.</param>
public record ErrorGeneratedDto([PublicAPI]AgvError Error) : DatedMessage;