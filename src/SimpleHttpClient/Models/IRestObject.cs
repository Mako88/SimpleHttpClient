using System.Collections.Generic;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// Base untyped RestObject
    /// </summary>
    public interface IRestObject
    {
        /// <summary>
        /// The headers
        /// </summary>
        Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The body as a string
        /// </summary>
        string StringBody { get; set; }
    }
}
