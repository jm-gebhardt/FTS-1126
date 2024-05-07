using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Dtos;

/// <summary>
/// Represents a dto that is returned when an <see cref="AgvError"/> has been generated with warning level.
/// </summary>
/// <param name="Warning">The generated warning.</param>
public record WarningGeneratedDto([PublicAPI]AgvError Warning) : DatedMessage;
