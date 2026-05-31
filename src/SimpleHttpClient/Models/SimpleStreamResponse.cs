using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// Read the response body as a sequence of text lines as they arrive. Lines are split on
        /// CR, LF, or CRLF. The encoding is taken from the response's Content-Type charset, falling
        /// back to UTF-8.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel reading (observed mid-read, not just between lines).</param>
        /// <returns>An async sequence of lines (without their line terminators).</returns>
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Wrap the body so the token passed here also cancels an in-progress read (the body may
            // already carry the request's token; linking is harmless and honors both).
            var stream = new CancellationAwareStream(Body, cancellationToken);

            // leaveOpen: this response owns Body and disposes it.
            using (var reader = new StreamReader(stream, GetEncoding(), detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

#if NET8_0_OR_GREATER
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
#endif
                    if (line == null)
                    {
                        yield break;
                    }

                    yield return line;
                }
            }
        }

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
        public async IAsyncEnumerable<ServerSentEvent> ReadServerSentEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var data = new StringBuilder();
            string eventType = null;
            string lastId = null;        // the SSE "last event id" carries forward across events
            int? retry = null;
            var hasData = false;

            await foreach (var line in ReadLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                // Blank line: dispatch the event accumulated so far.
                if (line.Length == 0)
                {
                    if (hasData)
                    {
                        // Remove the single trailing newline added after the last data line.
                        yield return new ServerSentEvent(data.ToString(0, data.Length - 1), eventType, lastId, retry);
                    }

                    data.Clear();
                    eventType = null;
                    retry = null;
                    hasData = false;
                    continue;
                }

                // Lines starting with a colon are comments (often keep-alive heartbeats); ignore.
                if (line[0] == ':')
                {
                    continue;
                }

                string field;
                string value;
                var colon = line.IndexOf(':');
                if (colon < 0)
                {
                    // A line with no colon is a field name with an empty value.
                    field = line;
                    value = string.Empty;
                }
                else
                {
                    field = line.Substring(0, colon);
                    value = line.Substring(colon + 1);
                    // A single leading space after the colon is part of the format, not the value.
                    if (value.Length > 0 && value[0] == ' ')
                    {
                        value = value.Substring(1);
                    }
                }

                switch (field)
                {
                    case "event":
                        eventType = value;
                        break;
                    case "data":
                        data.Append(value).Append('\n');
                        hasData = true;
                        break;
                    case "id":
                        // Per spec, ignore an id containing a NUL character.
                        if (value.IndexOf('\0') < 0)
                        {
                            lastId = value;
                        }
                        break;
                    case "retry":
                        if (int.TryParse(value, out var parsedRetry))
                        {
                            retry = parsedRetry;
                        }
                        break;
                    // Unknown fields are ignored per spec.
                }
            }

            // Per the SSE spec, an incomplete event at end-of-stream (no terminating blank line)
            // is not dispatched.
        }

        /// <summary>
        /// Determine the text encoding for the response from its Content-Type charset, defaulting
        /// to UTF-8 (which is also what the SSE spec mandates).
        /// </summary>
        private Encoding GetEncoding()
        {
            if (Headers != null &&
                Headers.TryGetValue("Content-Type", out var contentType) &&
                !string.IsNullOrEmpty(contentType))
            {
                var index = contentType.IndexOf("charset=", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var charset = contentType.Substring(index + "charset=".Length);

                    // Trim any following parameters and surrounding quotes/whitespace.
                    var semicolon = charset.IndexOf(';');
                    if (semicolon >= 0)
                    {
                        charset = charset.Substring(0, semicolon);
                    }

                    charset = charset.Trim().Trim('"', '\'');

                    if (charset.Length > 0)
                    {
                        try
                        {
                            return Encoding.GetEncoding(charset);
                        }
                        catch (ArgumentException)
                        {
                            // Unknown charset - fall through to the UTF-8 default.
                        }
                    }
                }
            }

            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

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
