using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// A streaming HTTP response that exposes the raw, unbuffered network stream.
    /// The underlying connection is held open until this object is disposed, so callers
    /// should dispose it (ideally with a <c>using</c> block) once they're done reading.
    /// </summary>
    public interface ISimpleStreamResponse : IDisposable
    {
        /// <summary>
        /// The response status code.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Whether or not the request was successful.
        /// </summary>
        bool IsSuccessful { get; }

        /// <summary>
        /// The response headers.
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The raw, unbuffered network stream containing the response body.
        /// Read from this directly to consume the response as it arrives.
        /// </summary>
        Stream Body { get; }
    }
}
