using System.Net.Http;

namespace SimpleHttpClient
{
    /// <summary>
    /// Creates the appropriate <see cref="IHttpClientProvider"/> for the target framework.
    /// This is the single place that varies by framework, keeping SimpleClient free of
    /// conditional compilation.
    /// </summary>
    internal static class HttpClientProviderFactory
    {
        /// <summary>
        /// Create a provider, optionally backed by an IHttpClientFactory.
        /// </summary>
        public static IHttpClientProvider Create(IHttpClientFactory httpClientFactory) =>
#if NETSTANDARD2_0
            new RotatingHttpClientProvider(httpClientFactory);
#else
            new PooledHttpClientProvider(httpClientFactory);
#endif
    }
}
