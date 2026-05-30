using Microsoft.Extensions.DependencyInjection;
using SimpleHttpClient.Logging;
using SimpleHttpClient.Serialization;
using System;

namespace SimpleHttpClient.Extensions
{
    /// <summary>
    /// Extensions for the IServiceCollection.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Register an IHttpClientFactory and ISimpleClient.
        /// </summary>
        public static IServiceCollection AddSimpleHttpClient(this IServiceCollection services,
            Func<IServiceProvider, ISimpleHttpSerializer> getSerializer = null,
            Func<IServiceProvider, ISimpleHttpLogger> getLogger = null)
        {
            services.AddHttpClient(Constants.HttpClientNameString, client => HttpClientConfigurator.ConfigureHttpClient(client))
            .ConfigurePrimaryHttpMessageHandler(x => HttpClientConfigurator.GetMessageHandler());

            if (getSerializer != null)
            {
                services.AddScoped(getSerializer);
            }

            if (getLogger != null)
            {
                services.AddScoped(getLogger);
            }

            // Registered as transient so each consumer gets its own client instance. This avoids
            // a shared, mutable Host being stomped on when multiple consumers share a scope.
            // For most scenarios, prefer injecting ISimpleClientFactory and calling CreateClient.
            services.AddTransient<ISimpleClient, SimpleClient>();
            services.AddSingleton<ISimpleClientFactory, SimpleClientFactory>();

            return services;
        }
    }
}
