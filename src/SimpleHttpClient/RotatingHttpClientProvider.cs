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
            var hasFactory = httpClientFactory != null;

            // With a factory on a non-Framework runtime, the factory pools and rotates handlers,
            // so just hand back a fresh client per request - no caching or rotation needed here.
            if (hasFactory && !IsDotNetFramework)
            {
                return CreateClient(useFactory: true);
            }

            // Otherwise (no factory, or on .NET Framework) cache a single client and rotate it
            // periodically to keep DNS fresh.
            lock (clientLock)
            {
                SetupTimerIfNeeded(hasFactory);

                if (httpClient == null)
                {
                    httpClient = CreateClient(hasFactory);
                }

                return httpClient;
            }
        }

        private static bool IsDotNetFramework =>
            RuntimeInformation.FrameworkDescription.Contains("Framework", StringComparison.OrdinalIgnoreCase);

        // Create either a factory-managed client or our own configured client.
        private HttpClient CreateClient(bool useFactory) =>
            useFactory
                ? httpClientFactory.CreateClient(Constants.HttpClientNameString)
                : HttpClientConfigurator.GetConfiguredHttpClient();

        // Callers must hold clientLock.
        private void SetupTimerIfNeeded(bool useFactory)
        {
            if (replacementTimer == null)
            {
                replacementTimer = new System.Timers.Timer();
                replacementTimer.Elapsed += (sender, e) => ReplaceClient(useFactory);
                replacementTimer.Interval = ReplacementIntervalMs;
                replacementTimer.AutoReset = true;
                replacementTimer.Start();
            }
        }

        private void ReplaceClient(bool useFactory)
        {
            HttpClient retiredClient;

            lock (clientLock)
            {
                retiredClient = httpClient;
                httpClient = CreateClient(useFactory);
            }

            // Only dispose clients we own. Factory-created clients are managed by the factory.
            if (retiredClient != null && !useFactory)
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
