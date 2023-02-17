using System;
using System.Net;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// An untyped HTTP response
    /// </summary>
    public class SimpleResponse : SimpleRestObject, ISimpleResponse
    {
        /// <summary>
        /// The response status code
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Whether or not the request was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
    }

    /// <summary>
    /// A typed HTTP response
    /// </summary>
    public class Response<T> : SimpleResponse, IResponse<T>
    {
        /// <summary>
        /// The typed response body
        /// </summary>
        public T Body { get; set; }

        /// <summary>
        /// The exception thrown (if any) when attempting to serialize the body
        /// </summary>
        public Exception SerializationException { get; set; }
    }
}
