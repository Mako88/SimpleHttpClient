using System;
using System.Net;
using System.Net.Http;

namespace SimpleHttpClient
{
    /// <summary>
    /// Class to configure an HttpClient.
    /// </summary>
    internal static class HttpClientConfigurator
    {
        /// <summary>
        /// Create a message handler with opinionated default settings.
        /// </summary>
        public static HttpMessageHandler GetMessageHandler()
        {
#if NETSTANDARD2_0
            var handler = new HttpClientHandler();

            // The checks/error handling below are thanks to Flurl's sourcecode
            try
            {
                // Disable cookies per https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
                handler.UseCookies = false;
            }
            catch (PlatformNotSupportedException)
            {
                // Do nothing
            }

            if (handler.SupportsRedirectConfiguration)
            {
                handler.AllowAutoRedirect = true;
            }

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            return handler;
#else
            // On modern runtimes SocketsHttpHandler rotates pooled connections on its own via
            // PooledConnectionLifetime, which keeps DNS fresh without replacing the HttpClient
            // (so none of the timer/replacement machinery the netstandard2.0 build needs).
            return new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                UseCookies = false,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
            };
#endif
        }

        /// <summary>
        /// Configure the given HttpClient with opinionated default settings.
        /// </summary>
        public static void ConfigureHttpClient(HttpClient client)
        {
            client.Timeout = TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite);
        }
    }
}
