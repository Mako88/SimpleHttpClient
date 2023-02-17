using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleHttpClient
{
    /// <summary>
    /// The primary Simple Http Client
    /// </summary>
    public class SimpleClient : ISimpleClient
    {
        /// <summary>
        /// Creates a SimpleClient instance.
        /// </summary>
        /// <param name="host">The base url all requests sent through this client will use. If not provided, it is assumed that the path property on requests passed to this client will be full URLs</param>
        /// <param name="httpClient">The HttpClient instance to use. If not provided, a new one is created</param>
        /// <param name="serializer">The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used</param>
        public SimpleClient(string host = null, HttpClient httpClient = null, ISimpleHttpSerializer serializer = null)
        {
            Host = host;

            // I'm pretty sure newing up an HttpClient isn't the best way to handle it
            // but apparently, there is no "best way": https://github.com/dotnet/aspnetcore/issues/28385#issuecomment-853766480
            // This is how RestSharp does it, so it's probably fine...
            HttpClient = httpClient ?? new HttpClient();

            Serializer = serializer ?? new SimpleHttpDefaultJsonSerializer();
        }

        /// <summary>
        /// The base url all requests sent with this client will use.
        /// If not set, it is assumed that the path property on requests passed to this client will be full URLs
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The HttpClient instance to use. If not set, a new one is created.
        /// See https://github.com/dotnet/aspnetcore/issues/28385#issuecomment-853766480 for a discussion on the proper way to create an HttpClient
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used
        /// </summary>
        public ISimpleHttpSerializer Serializer { get; set; }

        /// <summary>
        /// The Logger for logging requests and responses
        /// </summary>
        public ISimpleHttpLogger Logger { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes.
        /// This applies to all requests sent with this client
        /// </summary>
        public IEnumerable<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; set; } = new List<HttpStatusCode>();

        /// <summary>
        /// Headers that will be included with all requests sent with this client
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Make an untyped request
        /// </summary>
        /// <param name="request">The request that will be sent</param>
        /// <returns>A response object without a strongly-typed body property</returns>
        public async Task<ISimpleResponse> MakeRequest(ISimpleRequest request) =>
            await MakeRequestInternal(request, new SimpleResponse(), AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Make a typed request
        /// </summary>
        /// <typeparam name="T">The type the response body will be serialized into</typeparam>
        /// <param name="request">The request that will be sen</param>
        /// <returns>A response object with a strongly-typed body property</returns>
        public async Task<ISimpleResponse<T>> MakeRequest<T>(ISimpleRequest request) =>
            await MakeRequestInternal(request, new SimpleResponse<T>(), AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Get the URL the given request will be sent to by this client
        /// </summary>
        /// <param name="request">The request to determine the URL for</param>
        /// <returns>The URL the given request will be made to</returns>
        public string GetUrl(ISimpleRequest request) => CreateUrl(request);

        /// <summary>
        /// Execute a request
        /// </summary>
        private async Task<T> MakeRequestInternal<T>(ISimpleRequest request, T response, Func<HttpResponseMessage, T, Task> addResponseBody) where T : ISimpleResponse
        {
            var httpRequest = CreateHttpRequest(request);
            AddRequestBody(httpRequest, request);

            Logger?.LogRequest(httpRequest.RequestUri.ToString(), request);

            var httpResponse = await HttpClient.SendAsync(httpRequest).ConfigureAwait(false);

            PopulateResponse(httpResponse, response, request.AdditionalSuccessfulStatusCodes);
            await addResponseBody(httpResponse, response);

            Logger?.LogResponse(response);

            return response;
        }

        /// <summary>
        /// Create an HttpRequestMessage for use with HttpClient from an IRequest
        /// </summary>
        private HttpRequestMessage CreateHttpRequest(ISimpleRequest request)
        {
            var url = CreateUrl(request);

            var httpRequest = new HttpRequestMessage(request.Method, url);

            foreach (var header in DefaultHeaders.Concat(request.Headers))
            {
                httpRequest.Headers.Add(header.Key, header.Value);
            }

            return httpRequest;
        }

        /// <summary>
        /// Add a body to the request
        /// </summary>
        private void AddRequestBody(HttpRequestMessage httpRequest, ISimpleRequest request)
        {
            if (request.FormUrlEncodedParameters.Any())
            {
                request.ContentType = "application/x-www-form-urlencoded";

                httpRequest.Content = new FormUrlEncodedContent(request.FormUrlEncodedParameters);
            }
            else if (!string.IsNullOrEmpty(request.StringBody))
            {
                httpRequest.Content = new StringContent(request.StringBody, request.ContentEncoding, request.ContentType);
            }
            else if (request.Body != null)
            {
                httpRequest.Content = new StringContent(Serializer.Serialize(request.Body), request.ContentEncoding, request.ContentType);
            }
        }

        /// <summary>
        /// Add the body to the response
        /// </summary>
        private async Task AddResponseBody(HttpResponseMessage httpResponse, ISimpleResponse response)
        {
            response.StringBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            response.ByteBody = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Add the body to the response
        /// </summary>
        private async Task AddResponseBody<T>(HttpResponseMessage httpResponse, ISimpleResponse<T> response)
        {
            await AddResponseBody(httpResponse, (ISimpleResponse) response).ConfigureAwait(false);

            try
            {
                response.Body = Serializer.Deserialize<T>(response.StringBody);
            }
            catch (Exception ex)
            {
                response.SerializationException = ex;
            }
        }

        /// <summary>
        /// Create a response from an httpResponse
        /// </summary>
        private void PopulateResponse(HttpResponseMessage httpResponse, ISimpleResponse response, IEnumerable<HttpStatusCode> successfulStatusCodes)
        {
            response.Headers = new Dictionary<string, string>();

            foreach (var header in httpResponse.Headers.Concat(httpResponse.Content.Headers))
            {
                var value = string.Join(", ", header.Value);

                response.Headers.Add(header.Key, value);
            }

            response.StatusCode = httpResponse.StatusCode;
            response.IsSuccessful = httpResponse.IsSuccessStatusCode ||
                AdditionalSuccessfulStatusCodes.Concat(successfulStatusCodes).Any(x => x == httpResponse.StatusCode);
        }

        /// <summary>
        /// Create a URL for the given request
        /// </summary>
        private string CreateUrl(ISimpleRequest request)
        {
            var url = !string.IsNullOrWhiteSpace(request.OverrideUrl) ? request.OverrideUrl : CombineUrls(Host, request.Path);

            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            if (request.Method != HttpMethod.Post && request.Method != HttpMethod.Put)
            {
                foreach (var parameter in request.QueryStringParameters)
                {
                    query[parameter.Key] = parameter.Value;
                }
            }

            builder.Query = query.ToString();

            return builder.ToString();
        }

        /// <summary>
        /// Combine two urls, handling slashes
        /// </summary>
        private string CombineUrls(string url1, string url2)
        {
            if (string.IsNullOrWhiteSpace(url1))
            {
                return url2;
            }

            if (string.IsNullOrWhiteSpace(url2))
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return $"{url1}/{url2}";
        }
    }
}
