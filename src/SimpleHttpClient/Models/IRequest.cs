using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// The base untyped HTTP request
    /// </summary>
    public interface IRequest : IRestObject
    {
        /// <summary>
        /// The HTTP Request Method
        /// </summary>
        HttpMethod Method { get; set; }

        /// <summary>
        /// Query String Parameters on the request
        /// </summary>
        Dictionary<string, string> QueryStringParameters { get; set; }

        /// <summary>
        /// Form Url Encoded parameters for POST/PUT requests
        /// </summary>
        Dictionary<string, string> FormUrlEncodedParameters { get; set; }

        /// <summary>
        /// The request path that is appended to the client's host
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// A request URL to override the url set in the client
        /// </summary>
        string OverrideUrl { get; set; }

        /// <summary>
        /// The encoding for the content
        /// </summary>
        Encoding ContentEncoding { get; set; }

        /// <summary>
        /// The content type of the request
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// The body of the request
        /// </summary>
        object Body { get; set; }
    }
}
