using Gebhardt.Acc.Common.Messages;

namespace Gebhardt.Acc.CommonTest.Services.Filters;

public class Message : IMessageCreatedDate
{
    public Guid Key { get; } = Guid.NewGuid();

    public DateTime MessageCreatedDate { get; }

    public Message()
    {
    }

    public Message(DateTime messageCreatedDate)
    {
        MessageCreatedDate = messageCreatedDate;
    }
}