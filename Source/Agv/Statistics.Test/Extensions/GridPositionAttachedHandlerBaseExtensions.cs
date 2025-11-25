using System.Reflection;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Services.EventHandler;

namespace Gebhardt.Acc.Statistics.Test.Extensions;

public static class GridPositionAttachedHandlerBaseExtensions
{
    public static Task HandleTestMessageDirectly<T>(this GridCellAttachedHandlerBase<T> handler, T msg, List<GridCell> grids) where T : class, IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
    {
        var methodInfo = typeof(GridCellAttachedHandlerBase<T>)
            .GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return methodInfo.Invoke(handler, [msg, grids]) as Task ?? Task.CompletedTask;
    }
}