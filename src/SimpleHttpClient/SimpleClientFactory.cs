using Microsoft.Extensions.DependencyInjection;
using SimpleHttpClient.Logging;
using SimpleHttpClient.Serialization;
using System;
using System.Net.Http;

namespace SimpleHttpClient
{
    /// <summary>
    /// The default <see cref="ISimpleClientFactory"/> implementation. Resolves the
    /// IHttpClientFactory (and optional serializer/logger) from the service provider and
    /// hands each created client its own host.
    /// </summary>
    internal class SimpleClientFactory : ISimpleClientFactory
    {
        private readonly IServiceProvider services;

        /// <summary>
        /// Creates a SimpleClientFactory.
        /// </summary>
        /// <param name="services">The service provider used to resolve client dependencies.</param>
        public SimpleClientFactory(IServiceProvider services)
        {
            this.services = services;
        }

        /// <summary>
        /// Create a new <see cref="ISimpleClient"/> with the given host.
        /// </summary>
        public ISimpleClient CreateClient(string host = null) =>
            new SimpleClient(
                host,
                services.GetRequiredService<IHttpClientFactory>(),
                services.GetService<ISimpleHttpSerializer>(),
                services.GetService<ISimpleHttpLogger>());
    }
}
