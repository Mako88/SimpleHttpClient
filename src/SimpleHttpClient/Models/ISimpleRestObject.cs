using System.Collections.Generic;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// Base untyped RestObject
    /// </summary>
    public interface ISimpleRestObject
    {
        /// <summary>
        /// The headers
        /// </summary>
        Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The body as a string
        /// </summary>
        string StringBody { get; set; }

        /// <summary>
        /// An ID that is unique to a request/response pair
        /// </summary>
        string Id { get; }
    }
}
