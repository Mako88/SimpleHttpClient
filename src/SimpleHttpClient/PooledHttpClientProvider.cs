#if NET8_0_OR_GREATER
using System.Net.Http;

namespace SimpleHttpClient
{
    /// <summary>
    /// HttpClient provider for modern runtimes. The handler created by HttpClientConfigurator
    /// is a SocketsHttpHandler with a pooled connection lifetime, which keeps DNS fresh by
    /// rotating connections - so a single client can be created once and reused.
    /// </summary>
    internal sealed class PooledHttpClientProvider : IHttpClientProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly object clientLock = new object();

        private HttpClient httpClient;
        private bool disposedValue;

        public PooledHttpClientProvider(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public HttpClient GetClient()
        {
            if (httpClientFactory != null)
            {
                return httpClientFactory.CreateClient(Constants.HttpClientNameString);
            }

            lock (clientLock)
            {
                if (httpClient == null)
                {
                    var handler = HttpClientConfigurator.GetMessageHandler();

                    httpClient = new HttpClient(handler);

                    HttpClientConfigurator.ConfigureHttpClient(httpClient);
                }

                return httpClient;
            }
        }

        public void Dispose()
        {
            if (disposedValue)
            {
                return;
            }

            // Only dispose a client we created ourselves; factory-created clients are owned by the factory.
            httpClient?.Dispose();
            disposedValue = true;
        }
    }
}
#endif
