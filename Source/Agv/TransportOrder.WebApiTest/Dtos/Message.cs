
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Dtos;

public class Message(DateTime messageCreatedDate) : IMessageCreatedDate
{
    public DateTime MessageCreatedDate { get; } = messageCreatedDate;
}