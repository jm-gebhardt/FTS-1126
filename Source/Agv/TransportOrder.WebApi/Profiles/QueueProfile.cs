using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for mapping queues
/// </summary>
public class QueueProfile : WebApiProfile
{
    /// <inheritdoc cref="QueueProfile"/>>
    public QueueProfile()
    {
        CreateMap<QueueNode, QueueNodeDto>()
            .ForSourceMember(s => s.Node, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.State, opt => opt.DoNotValidate());
        CreateMap<QueueNodeDto, QueueNode>()
            .ForMember(m => m.Node, opt => opt.Ignore())
            .ForMember(m => m.State, opt => opt.Ignore());
        CreateMap<QueueNodeDto, Common.Messages.Queue.QueueNode>();
        CreateMap<List<QueueNodeDto>, List<Common.Messages.Queue.QueueNode>>().ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, _) => context.Mapper.Map<Common.Messages.Queue.QueueNode>(dto)).ToList();
            });
        CreateMap<Queue, QueueDto>()
            .ForSourceMember(s => s.Head , opt => opt.DoNotValidate())
            .ForSourceMember(s => s.AgvFillQuantity , opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Tail , opt => opt.DoNotValidate())
            .ForSourceMember(s => s.IsFull, opt => opt.DoNotValidate());
    }
}