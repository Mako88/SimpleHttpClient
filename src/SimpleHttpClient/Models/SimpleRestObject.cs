using System.Collections.Generic;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// Base untyped RestObject
    /// </summary>
    public class SimpleRestObject : ISimpleRestObject
    {
        /// <summary>
        /// The headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The body as a string
        /// </summary>
        public string StringBody { get; set; }
    }
}
