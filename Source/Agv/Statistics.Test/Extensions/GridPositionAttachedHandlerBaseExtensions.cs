using System.Reflection;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.Statistics.Services.EventHandler;

namespace Gebhardt.Acc.Statistics.Test.Extensions;

public static class GridPositionAttachedHandlerBaseExtensions
{
    public static Task HandleTestMessageDirectly<T>(this GridPositionAttachedHandlerBase<T> handler, T msg, List<Guid> gridIds) where T : class, IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
    {
        var methodInfo = typeof(GridPositionAttachedHandlerBase<T>)
            .GetMethod("HandleWithGridIdsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return methodInfo.Invoke(handler, [msg, gridIds]) as Task ?? Task.CompletedTask;
    }
}