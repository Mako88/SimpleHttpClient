namespace SimpleHttpClient.Models
{
    /// <summary>
    /// A single event parsed from a <c>text/event-stream</c> (Server-Sent Events) response,
    /// per the WHATWG/W3C SSE specification. This represents the transport-level frame only;
    /// application-specific conventions (such as a sentinel <c>data</c> value that marks the end
    /// of the stream, or deserializing <see cref="Data"/> into a type) are left to the caller.
    /// </summary>
    public sealed class ServerSentEvent
    {
        /// <summary>
        /// Creates a Server-Sent Event.
        /// </summary>
        /// <param name="data">The event data (multiple <c>data:</c> lines joined with newlines, trailing newline removed).</param>
        /// <param name="eventType">The event type from the <c>event:</c> field, or null if none was specified.</param>
        /// <param name="id">The last event id (from an <c>id:</c> field); per the SSE spec this value carries forward to later events until changed.</param>
        /// <param name="retry">The reconnection time in milliseconds from a <c>retry:</c> field, or null if none was specified.</param>
        public ServerSentEvent(string data, string eventType = null, string id = null, int? retry = null)
        {
            Data = data;
            EventType = eventType;
            Id = id;
            Retry = retry;
        }

        /// <summary>
        /// The event data. When an event contains multiple <c>data:</c> lines they are joined
        /// with newline characters, and the single trailing newline is removed.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// The event type from the <c>event:</c> field, or null if the event didn't specify one.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        /// The last event id. Per the SSE specification the id "carries forward": once set by an
        /// <c>id:</c> field it applies to subsequent events too, until a later <c>id:</c> changes it.
        /// Null if no id has been seen yet.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The reconnection time in milliseconds from a <c>retry:</c> field, or null if the event
        /// didn't specify one.
        /// </summary>
        public int? Retry { get; }
    }
}
