using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IMediatorRegistrationConfigurator"/>.
/// </summary>
public static class MediatorRegistrationExtension
{
    private const int PartitionCount = 16;

    extension(IMediatorRegistrationConfigurator configurator)
    {
        /// <summary>
        /// Add the mediator
        /// </summary>
         // ReSharper disable once UnusedMethodReturnValue.Global
        public IMediatorRegistrationConfigurator AddMediator()
        {
            configurator.AddRequestClient<UploadLif>();
            configurator.AddRequestClient<GetLifFiles>();
            configurator.AddRequestClient<GetLifFile>();
            configurator.AddRequestClient<DeleteLifFile>();
            configurator.AddRequestClient<DownloadLifFile>();
            configurator.AddRequestClient<AddOrUpdateLif>();
            configurator.AddConsumer<LifRequestHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                partitioner.PartitionMessageById<DeleteLifFile, LifRequestHandler>(c);
            });
            configurator.AddRequestClient<AddOrUpdateFactsheet>();
            configurator.AddConsumer<FactsheetRequestHandler>();
            return configurator;
        }
    }
}