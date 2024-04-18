using System;
using System.Collections.Generic;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// Base untyped RestObject.
    /// </summary>
    public class SimpleRestObject : ISimpleRestObject
    {
        /// <summary>
        /// The headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The body as a string.
        /// </summary>
        public string StringBody { get; set; }

        /// <summary>
        /// An ID that is unique to a request/response pair.
        /// </summary>
        public string Id { get; protected set; }
    }
}
