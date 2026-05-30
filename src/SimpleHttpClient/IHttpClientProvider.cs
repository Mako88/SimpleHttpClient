using System;
using System.Net.Http;

namespace SimpleHttpClient
{
    /// <summary>
    /// Provides the HttpClient used to send requests, encapsulating the platform-specific
    /// strategy for keeping connections and DNS fresh over the client's lifetime.
    /// </summary>
    internal interface IHttpClientProvider : IDisposable
    {
        /// <summary>
        /// Get an HttpClient to send a request with.
        /// </summary>
        HttpClient GetClient();
    }
}
