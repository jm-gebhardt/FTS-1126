using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="MaintenanceLog" />
/// </summary>
public class AgvMaintenanceLogProfile : WebApiProfile
{
    /// <inheritdoc cref="AgvMaintenanceLogProfile"/>>
    public AgvMaintenanceLogProfile()
    {
        CreateMap<MaintenanceLog, AgvMaintenanceLogDto>()
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate());
    }
}