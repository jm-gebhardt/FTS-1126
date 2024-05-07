using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Dtos;

/// <summary>
/// Represents a dto that is returned when an <see cref="AgvError"/> has been generated with fatal level.
/// </summary>
/// <param name="Error">The generated error.</param>
public record ErrorGeneratedDto([PublicAPI]AgvError Error) : DatedMessage;