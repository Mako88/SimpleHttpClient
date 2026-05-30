using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// A streaming HTTP response that exposes the raw, unbuffered network stream.
    /// </summary>
    public class SimpleStreamResponse : ISimpleStreamResponse
    {
        private readonly HttpResponseMessage httpResponse;
        private bool disposedValue;

        /// <summary>
        /// Creates a stream response that wraps the given HttpResponseMessage and body stream.
        /// The HttpResponseMessage is owned by this object and disposed along with it.
        /// </summary>
        /// <param name="httpResponse">The HttpResponseMessage whose lifetime is tied to this response.</param>
        /// <param name="body">The raw, unbuffered network stream containing the response body.</param>
        public SimpleStreamResponse(HttpResponseMessage httpResponse, Stream body)
        {
            this.httpResponse = httpResponse;
            Body = body;
        }

        /// <summary>
        /// The response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Whether or not the request was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// The response headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The raw, unbuffered network stream containing the response body.
        /// </summary>
        public Stream Body { get; private set; }

        /// <summary>
        /// Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Body?.Dispose();
                    httpResponse?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose, releasing the underlying network stream and connection.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
