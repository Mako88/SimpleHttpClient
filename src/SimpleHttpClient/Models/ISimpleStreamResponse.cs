using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

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

        /// <summary>
        /// Read the response body as a sequence of text lines as they arrive. Lines are split on
        /// CR, LF, or CRLF. The encoding is taken from the response's Content-Type charset, falling
        /// back to UTF-8.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel reading (observed mid-read, not just between lines).</param>
        /// <returns>An async sequence of lines (without their line terminators).</returns>
        IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Read the response body as a sequence of Server-Sent Events (<c>text/event-stream</c>),
        /// parsing the SSE wire format per the WHATWG/W3C specification: events are separated by
        /// blank lines, multiple <c>data:</c> lines are joined with newlines, and lines beginning
        /// with a colon (comments / keep-alives) are ignored.
        /// <para>
        /// Application-specific conventions are intentionally NOT handled here - for example a
        /// sentinel <c>data</c> value that marks the end of the stream, or deserializing
        /// <see cref="ServerSentEvent.Data"/>. Inspect each event and handle those in your own loop.
        /// </para>
        /// </summary>
        /// <param name="cancellationToken">A token to cancel reading.</param>
        /// <returns>An async sequence of <see cref="ServerSentEvent"/>.</returns>
        IAsyncEnumerable<ServerSentEvent> ReadServerSentEventsAsync(CancellationToken cancellationToken = default);
    }
}
