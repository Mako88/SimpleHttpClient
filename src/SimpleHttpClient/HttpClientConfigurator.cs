using System;
using System.Net;
using System.Net.Http;

namespace SimpleHttpClient
{
    /// <summary>
    /// Class to configure an HttpClient
    /// </summary>
    internal static class HttpClientConfigurator
    {
        /// <summary>
        /// Create a message handler with opinionated default settings
        /// </summary>
        public static HttpClientHandler GetMessageHandler()
        {
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
        }

        /// <summary>
        /// Configure the given HttpClient with opinionated default settings
        /// </summary>
        public static void ConfigureHttpClient(HttpClient client)
        {
            client.Timeout = TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite);
        }
    }
}
