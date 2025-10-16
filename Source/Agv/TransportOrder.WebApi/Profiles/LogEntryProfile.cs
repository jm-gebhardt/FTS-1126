using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="LogEntry"/>.
/// </summary>
[UsedImplicitly]
public class LogEntryProfile : WebApiProfile
{
    /// <inheritdoc cref="LogEntryProfile"/>>
    public LogEntryProfile()
    {
        CreateMap<LogEntry, LogEntryDto>()
            .ForSourceMember(l => l.Id, opt => opt.DoNotValidate());
    }
}