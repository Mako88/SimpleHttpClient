#if NETSTANDARD2_0
using SimpleHttpClient.Extensions;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SimpleHttpClient
{
    /// <summary>
    /// HttpClient provider for netstandard2.0 (and .NET Framework), which can't use
    /// SocketsHttpHandler.PooledConnectionLifetime. To keep DNS fresh it periodically replaces
    /// the HttpClient, disposing the retired one after a grace period so in-flight requests can
    /// finish. Ref: https://github.com/dotnet/runtime/issues/18348
    /// </summary>
    internal sealed class RotatingHttpClientProvider : IHttpClientProvider
    {
        private const double ReplacementIntervalMs = 300000; // 5 minutes

        // How long a retired HttpClient is kept alive before being disposed, so in-flight
        // requests (and reasonably-lived streams) using it can finish first.
        private const int DisposeDelayMs = 300000; // 5 minutes

        private readonly IHttpClientFactory httpClientFactory;
        private readonly object clientLock = new object();

        private HttpClient httpClient;
        private System.Timers.Timer replacementTimer;
        private bool disposedValue;

        public RotatingHttpClientProvider(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public HttpClient GetClient()
        {
            // Without a factory, new up our own client and rotate it periodically.
            if (httpClientFactory == null)
            {
                lock (clientLock)
                {
                    SetupTimerIfNeeded(false);

                    if (httpClient == null)
                    {
                        httpClient = CreateConfiguredClient();
                    }

                    return httpClient;
                }
            }

            // Don't create a new HttpClient per request on .NET Framework; cache one and rotate it.
            if (RuntimeInformation.FrameworkDescription.Contains("Framework", StringComparison.OrdinalIgnoreCase))
            {
                lock (clientLock)
                {
                    SetupTimerIfNeeded(true);

                    if (httpClient == null)
                    {
                        httpClient = httpClientFactory.CreateClient(Constants.HttpClientNameString);
                    }

                    return httpClient;
                }
            }

            return httpClientFactory.CreateClient(Constants.HttpClientNameString);
        }

        private static HttpClient CreateConfiguredClient()
        {
            var handler = HttpClientConfigurator.GetMessageHandler();

            var client = new HttpClient(handler);

            HttpClientConfigurator.ConfigureHttpClient(client);

            return client;
        }

        // Callers must hold clientLock.
        private void SetupTimerIfNeeded(bool shouldUseFactory)
        {
            if (replacementTimer == null)
            {
                replacementTimer = new System.Timers.Timer();
                replacementTimer.Elapsed += (sender, e) => ReplaceClient(shouldUseFactory);
                replacementTimer.Interval = ReplacementIntervalMs;
                replacementTimer.AutoReset = true;
                replacementTimer.Start();
            }
        }

        private void ReplaceClient(bool shouldUseFactory)
        {
            HttpClient retiredClient;

            lock (clientLock)
            {
                retiredClient = httpClient;

                httpClient = shouldUseFactory
                    ? httpClientFactory.CreateClient(Constants.HttpClientNameString)
                    : CreateConfiguredClient();
            }

            // Only dispose clients we own. Factory-created clients are managed by the factory.
            if (retiredClient != null && !shouldUseFactory)
            {
                _ = DisposeAfterDelayAsync(retiredClient);
            }
        }

        private async Task DisposeAfterDelayAsync(HttpClient retiredClient)
        {
            await Task.Delay(DisposeDelayMs).ConfigureAwait(false);

            retiredClient.Dispose();
        }

        public void Dispose()
        {
            if (disposedValue)
            {
                return;
            }

            replacementTimer?.Stop();
            replacementTimer?.Dispose();
            httpClient?.Dispose();
            disposedValue = true;
        }
    }
}
#endif
