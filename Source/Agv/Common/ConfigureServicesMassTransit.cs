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
        /// ConfigureServices
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration</param>
        public static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            string user = config["RabbitMQ:User"];
            string password = config["RabbitMQ:Password"];
            string address = config["RabbitMQ:Host"];
            string port = config["RabbitMQ:Port"];

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                    cfg.Host($"amqp://{user}:{password}@{address}:{port}");
                });                
            });
        }
    }
}