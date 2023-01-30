using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly string host;
        private readonly HttpClient httpClient;
        private readonly ISimpleHttpSerializer serializer;
        private readonly ISimpleHttpLogger logger;

        /// <summary>
        /// Constructor
        /// </summary>
        public SimpleClient(string host = null, HttpClient httpClient = null, ISimpleHttpSerializer serializer = null, ISimpleHttpLogger logger = null)
        {
            this.host = host;

            // I'm pretty sure newing up an HttpClient isn't the best way to handle it
            // but apparently, there is no "best way": https://github.com/dotnet/aspnetcore/issues/28385#issuecomment-853766480
            // This is how RestSharp does it, so it's probably fine...
            this.httpClient = httpClient ?? new HttpClient();

            this.serializer = serializer ?? new SimpleHttpDefaultJsonSerializer();
            this.logger = logger;
        }

        /// <summary>
        /// Execute a request
        /// </summary>
        public async Task<IResponse> MakeRequest(IRequest request) =>
            await MakeRequestInternal(request, new Response(), AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Execute a request
        /// </summary>
        public async Task<IResponse<T>> MakeRequest<T>(IRequest request) =>
            await MakeRequestInternal(request, new Response<T>(), AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Execute a request
        /// </summary>
        private async Task<T> MakeRequestInternal<T>(IRequest request, T response, Func<HttpResponseMessage, T, Task> addResponseBody) where T : IResponse
        {
            var httpRequest = CreateHttpRequest(request);
            AddRequestBody(httpRequest, request);

            logger?.LogRequest(httpRequest.RequestUri.ToString(), request);

            var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);

            PopulateResponse(httpResponse, response);
            await addResponseBody(httpResponse, response);

            logger?.LogResponse(response);

            return response;
        }

        /// <summary>
        /// Create an HttpRequestMessage for use with HttpClient from an IRequest
        /// </summary>
        private HttpRequestMessage CreateHttpRequest(IRequest request)
        {
            var url = CreateUrl(request);

            var httpRequest = new HttpRequestMessage(request.Method, url);

            foreach (var header in request.Headers)
            {
                httpRequest.Headers.Add(header.Key, header.Value);
            }

            return httpRequest;
        }

        /// <summary>
        /// Add a body to the request
        /// </summary>
        private void AddRequestBody(HttpRequestMessage httpRequest, IRequest request)
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
                httpRequest.Content = new StringContent(serializer.Serialize(request.Body), request.ContentEncoding, request.ContentType);
            }
        }

        /// <summary>
        /// Add the body to the response
        /// </summary>
        private async Task AddResponseBody(HttpResponseMessage httpResponse, IResponse response)
        {
            response.StringBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Add the body to the response
        /// </summary>
        private async Task AddResponseBody<T>(HttpResponseMessage httpResponse, IResponse<T> response)
        {
            await AddResponseBody(httpResponse, (IResponse) response).ConfigureAwait(false);

            try
            {
                response.Body = serializer.Deserialize<T>(response.StringBody);
            }
            catch (Exception ex)
            {
                response.SerializationException = ex;
            }
        }

        /// <summary>
        /// Create a response from an httpResponse
        /// </summary>
        private void PopulateResponse(HttpResponseMessage httpResponse, IResponse response)
        {
            response.Headers = new Dictionary<string, string>();

            foreach (var header in httpResponse.Headers.Concat(httpResponse.Content.Headers))
            {
                var value = string.Join(", ", header.Value);

                response.Headers.Add(header.Key, value);
            }

            response.StatusCode = httpResponse.StatusCode;
        }

        /// <summary>
        /// Create a URL for the given request
        /// </summary>
        private string CreateUrl(IRequest request)
        {
            var url = !string.IsNullOrWhiteSpace(request.OverrideUrl) ? request.OverrideUrl : CombineUrls(host, request.Path);

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
