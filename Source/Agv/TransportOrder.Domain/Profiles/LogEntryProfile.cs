using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for mapping <see cref="LogEntry"/>.
/// </summary>
public class LogEntryProfile: Profile
{
    /// <inheritdoc cref="LogEntryProfile"/>>
    public LogEntryProfile()
    {
        CreateMap<LogEntry, LogEntryDto>().ReverseMap();
    }
}