using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// An untyped HTTP request
    /// </summary>
    public class Request : RestObject, IRequest
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Request(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Request(string path, HttpMethod method) : this(path)
        {
            Method = method;
        }

        /// <summary>
        /// The HTTP Request Method
        /// </summary>
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Query String Parameters on the request
        /// </summary>
        public Dictionary<string, string> QueryStringParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The request path that is appended to the client's host
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// A request URL to override the url set in the client
        /// </summary>
        public string OverrideUrl { get; set; }

        /// <summary>
        /// The encoding for the content
        /// </summary>
        public Encoding ContentEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// The content type of the request
        /// </summary>
        public string ContentType { get; set; } = "application/json";
    }

    /// <summary>
    /// A typed HTTP request
    /// </summary>
    public class Request<T> : Request, IRequest<T>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Request(string path, T body) : base(path, HttpMethod.Post)
        {
            Body = body;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Request(string path, HttpMethod method, T body) : base(path, method)
        {
            Body = body;
        }

        /// <summary>
        /// The typed request body
        /// </summary>
        public T Body { get; set; }
    }
}
