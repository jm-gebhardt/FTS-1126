using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// MassTransit configurations
    /// </summary>
    public static class ConfigureServicesMassTransit
    {
  
        /// <summary>
        /// MassTransit Configuration required for RabbitMQ
        /// </summary>
        public record MassTransitConfiguration
        {
            /// <summary>
            /// Host
            /// </summary>
            public string? Host { get; set; }
            /// <summary>
            /// User
            /// </summary>
            public string? User { get; set; }
            /// <summary>
            /// Password
            /// </summary>
            public string? Password { get; set; }
            /// <summary>
            /// Port
            /// </summary>
            public string? Port { get; set; }
            /// <summary>
            /// Create RabbitMQ URL
            /// </summary>
            public string Url => $"amqp://{User}:{Password}@{Host}:{Port}";
        }

        /// <summary>
        /// Expected configuration section name for MassTransit
        /// </summary>
        public const string ConfigSectionName = "RabbitMQ";
        /// <summary>
        /// ConfigureServices
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration</param>
        public static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            var massTransitConfig = config.GetRequiredSection(ConfigSectionName).Get<MassTransitConfiguration>();
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                    cfg.Host(massTransitConfig.Url);
                });                
            });
        }
    }
}