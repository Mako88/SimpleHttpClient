using SimpleHttpClient.Extensions;
using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SimpleHttpClient
{
    /// <summary>
    /// The primary Simple Http Client
    /// </summary>
    public class SimpleClient : ISimpleClient, IDisposable
    {
        private readonly IHttpClientProvider httpClientProvider;
        private bool disposedValue;

        /// <summary>
        /// Creates a SimpleClient instance.
        /// </summary>
        /// <param name="host">The base url all requests sent through this client will use. If not provided, it is assumed that the path property on requests passed to this client will be full URLs.</param>
        /// <param name="httpClientFactory">An IHttpClientFactory to create HttpClients. This is resolved through dependency injection when using the IServiceCollection.AddSimpleHttpClient() extension method.</param>
        /// <param name="serializer">The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used.</param>
        /// <param name="logger">The logger used for logging requests and responses.</param>
        /// <param name="logRequest">An optional method for logging a request. This is called right before the request is sent.</param>
        /// <param name="logResponse">An optional method for logging a response. This is called right after a response is received.</param>
        public SimpleClient(string host = null,
            IHttpClientFactory httpClientFactory = null,
            ISimpleHttpSerializer serializer = null,
            ISimpleHttpLogger logger = null,
            LogRequest logRequest = null,
            LogResponse logResponse = null)
        {
            httpClientProvider = HttpClientProviderFactory.Create(httpClientFactory);

            Host = host;
            Serializer = serializer ?? new SimpleHttpDefaultJsonSerializer();
            Logger = logger;
            LogRequest = logRequest;
            LogResponse = logResponse;
        }

        /// <summary>
        /// The base url all requests sent with this client will use.
        /// If not set, it is assumed that the path property on requests passed to this client will be full URLs.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used.
        /// </summary>
        public ISimpleHttpSerializer Serializer { get; set; }

        /// <summary>
        /// The Logger for logging requests and responses.
        /// </summary>
        public ISimpleHttpLogger Logger { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes.
        /// This applies to all requests sent with this client.
        /// </summary>
        public List<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; private set; } = new List<HttpStatusCode>();

        /// <summary>
        /// Headers that will be included with all requests sent with this client.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Timeout in seconds of all requests sent with this client.
        /// To disable the timeout, set to -1.
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// An optional action for logging a request.
        /// This is called right before the request is sent.
        /// </summary>
        public LogRequest LogRequest { get; set; }

        /// <summary>
        /// An optional action for logging a response.
        /// This is called right after a response is received.
        /// </summary>
        public LogResponse LogResponse { get; set; }

        /// <summary>
        /// Make an untyped request.
        /// </summary>
        /// <param name="request">The request that will be sent.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A response object without a strongly-typed body property.</returns>
        public async Task<ISimpleResponse> MakeRequest(ISimpleRequest request, CancellationToken cancellationToken = default) =>
            await MakeRequestInternal(request, new SimpleResponse(request.Id), AddResponseBody, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Make a typed request.
        /// </summary>
        /// <typeparam name="T">The type the response body will be serialized into.</typeparam>
        /// <param name="request">The request that will be sent.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A response object with a strongly-typed body property.</returns>
        public async Task<ISimpleResponse<T>> MakeRequest<T>(ISimpleRequest request, CancellationToken cancellationToken = default) =>
            await MakeRequestInternal(request, new SimpleResponse<T>(request.Id), AddResponseBody, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Make a request and get back the live, unbuffered response stream.
        /// The body is not read into memory; the connection is held open until the
        /// returned ISimpleStreamResponse is disposed, so callers should dispose it
        /// (ideally with a using block) once they're done reading.
        /// </summary>
        /// <param name="request">The request that will be sent.</param>
        /// <param name="cancellationToken">A token to cancel sending the request and reading the response stream.</param>
        /// <returns>A disposable response exposing the raw response stream.</returns>
        public async Task<ISimpleStreamResponse> MakeStreamRequest(ISimpleRequest request, CancellationToken cancellationToken = default)
        {
            var httpRequest = CreateHttpRequest(request);
            AddRequestBody(httpRequest, request);
            ApplyHeaders(httpRequest, request);

            var url = httpRequest.RequestUri.ToString();

            Logger?.LogRequest(url, request);

            if (LogRequest != null)
            {
                LogRequest(url, request);
            }

            // ResponseHeadersRead so SendAsync returns as soon as the headers are
            // available instead of buffering the whole body, which is what lets us stream.
            var httpResponse = await SendHttpRequest(request, httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            var body = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

            // The HttpResponseMessage is handed to the response so its lifetime (and the
            // underlying connection) is controlled by the caller disposing the response.
            var response = new SimpleStreamResponse(httpResponse, body)
            {
                StatusCode = httpResponse.StatusCode,
                IsSuccessful = ResponseIsSuccessful(httpResponse, request.AdditionalSuccessfulStatusCodes),
            };

            PopulateHeaders(httpResponse, response.Headers);

            return response;
        }

        /// <summary>
        /// Get the URL the given request will be sent to by this client.
        /// </summary>
        /// <param name="request">The request to determine the URL for.</param>
        /// <returns>The URL the given request will be made to.</returns>
        public string GetUrl(ISimpleRequest request) => CreateUrl(request);

        /// <summary>
        /// Execute a request.
        /// </summary>
        private async Task<T> MakeRequestInternal<T>(ISimpleRequest request, T response, Func<HttpResponseMessage, T, ISimpleHttpSerializer, Task> addResponseBody, CancellationToken cancellationToken) where T : ISimpleResponse
        {
            var httpRequest = CreateHttpRequest(request);
            AddRequestBody(httpRequest, request);
            ApplyHeaders(httpRequest, request);

            var url = httpRequest.RequestUri.ToString();

            Logger?.LogRequest(url, request);

            if (LogRequest != null)
            {
                LogRequest(url, request);
            }

            var httpResponse = await SendHttpRequest(request, httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

            PopulateResponse(httpResponse, response, request.AdditionalSuccessfulStatusCodes);
            await addResponseBody(httpResponse, response, request.SerializerOverride ?? Serializer);

            Logger?.LogResponse(response);

            if (LogResponse != null)
            {
                LogResponse(response);
            }

            return response;
        }

        /// <summary>
        /// Send an HttpRequestMessage, applying the request/client timeout to the send and
        /// honoring the caller's cancellation token. A timeout surfaces as a TimeoutException;
        /// a caller-requested cancellation propagates as an OperationCanceledException.
        /// </summary>
        private async Task<HttpResponseMessage> SendHttpRequest(ISimpleRequest request, HttpRequestMessage httpRequest, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            var timeout = request.TimeoutOverride ?? Timeout;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                if (timeout != -1)
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(timeout));
                }

                try
                {
                    return await httpClientProvider.GetClient().SendAsync(httpRequest, completionOption, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Request timed out after {timeout} seconds");
                }
                catch (ProtocolViolationException ex)
                {
                    // .NET Framework's HttpClient (backed by HttpWebRequest) rejects a request body
                    // on methods that don't allow one - most commonly a GET with a body. Surface a
                    // clearer, actionable error than the raw ProtocolViolationException.
                    throw new NotSupportedException(
                        "Sending a request body with this HTTP method isn't supported on this platform. " +
                        ".NET Framework's HttpClient rejects it (for example, a GET request with a body); " +
                        "use a body-bearing method such as POST or PUT, or target a modern runtime.", ex);
                }
            }
        }

        /// <summary>
        /// Create an HttpRequestMessage for use with HttpClient from an IRequest.
        /// </summary>
        private HttpRequestMessage CreateHttpRequest(ISimpleRequest request) =>
            new HttpRequestMessage(request.Method, CreateUrl(request));

        /// <summary>
        /// Merge the request headers with the client's default headers, with the request's
        /// headers taking precedence on any conflicts.
        /// </summary>
        private Dictionary<string, string> MergeHeaders(ISimpleRequest request) =>
            request.Headers.Concat(DefaultHeaders.Where(x => !request.Headers.Keys.Contains(x.Key)))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Apply the merged request/default headers to the HttpRequestMessage. Must be called
        /// after the request body has been set so content-level headers can be applied to it.
        /// </summary>
        private void ApplyHeaders(HttpRequestMessage httpRequest, ISimpleRequest request)
        {
            var headers = MergeHeaders(request);

            if (!headers.Keys.Contains("User-Agent", StringComparer.OrdinalIgnoreCase))
            {
                httpRequest.Headers.TryAddWithoutValidation("User-Agent", Constants.DefaultUserAgent);
            }

            foreach (var header in headers)
            {
                // Content-Type is applied to the request body content (see AddRequestBody), not here.
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // TryAddWithoutValidation avoids throwing on header values HttpClient would
                // otherwise reject. Content-level headers can't go on the request headers, so
                // fall back to applying them to the body content where they belong.
                if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Add a body to the request.
        /// </summary>
        private void AddRequestBody(HttpRequestMessage httpRequest, ISimpleRequest request)
        {
            // A Content-Type header overrides the default content type (but never an explicitly-set
            // one). Reflect the resolved value back onto the request so it shows what was sent.
            if (request.ContentType == Constants.DefaultContentType &&
                MergeHeaders(request).TryGetValue("Content-Type", out var headerContentType))
            {
                request.ContentType = headerContentType;
            }

            if (request.FormUrlEncodedParameters.Any())
            {
                httpRequest.Content = new FormUrlEncodedContent(request.FormUrlEncodedParameters);
            }
            else if (request.Body is string stringBody)
            {
                // A string body is sent as-is - never re-serialized.
                httpRequest.Content = new StringContent(stringBody, request.ContentEncoding, request.ContentType);

                request.StringBody = stringBody;
            }
            else if (request.Body != null)
            {
                // An object Body is the source of truth: it's serialized on every send (so re-sending
                // after changing Body sends the new value), and the serialized form is reflected back
                // onto StringBody.
                var serializer = request.SerializerOverride ?? Serializer;

                var serializedBody = serializer.Serialize(request.Body);

                httpRequest.Content = new StringContent(serializedBody, request.ContentEncoding, request.ContentType);

                request.StringBody = serializedBody;
            }
            else if (!string.IsNullOrEmpty(request.StringBody))
            {
                // No Body, but a string body was set directly on the request.
                httpRequest.Content = new StringContent(request.StringBody, request.ContentEncoding, request.ContentType);
            }
        }

        /// <summary>
        /// Add the body to the response.
        /// </summary>
        private async Task AddResponseBody(HttpResponseMessage httpResponse, ISimpleResponse response, ISimpleHttpSerializer serializer)
        {
            // Read the content once as bytes, then decode the string from those bytes, rather than
            // reading (and copying) the whole body twice.
            var bytes = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            response.ByteBody = bytes;
            response.StringBody = DecodeResponseBody(httpResponse.Content, bytes);
        }

        /// <summary>
        /// Decode response bytes to a string using the response's charset (falling back to UTF-8),
        /// honoring a byte-order mark if present - matching HttpContent.ReadAsStringAsync closely.
        /// </summary>
        private static string DecodeResponseBody(HttpContent content, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            Encoding encoding = null;
            var charSet = content.Headers.ContentType?.CharSet;
            if (!string.IsNullOrWhiteSpace(charSet))
            {
                try
                {
                    encoding = Encoding.GetEncoding(charSet.Trim('"', '\'', ' '));
                }
                catch (ArgumentException)
                {
                    // Unknown/invalid charset - fall back to the default encoding below.
                }
            }

            using (var stream = new MemoryStream(bytes))
            using (var reader = new StreamReader(stream, encoding ?? Constants.DefaultEncoding, detectEncodingFromByteOrderMarks: true))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Add the body to the response.
        /// </summary>
        private async Task AddResponseBody<T>(HttpResponseMessage httpResponse, ISimpleResponse<T> response, ISimpleHttpSerializer serializer)
        {
            await AddResponseBody(httpResponse, (ISimpleResponse) response, serializer).ConfigureAwait(false);

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
        /// Create a response from an httpResponse.
        /// </summary>
        private void PopulateResponse(HttpResponseMessage httpResponse, ISimpleResponse response, IEnumerable<HttpStatusCode> successfulStatusCodes)
        {
            PopulateHeaders(httpResponse, response.Headers);

            response.StatusCode = httpResponse.StatusCode;
            response.IsSuccessful = ResponseIsSuccessful(httpResponse, successfulStatusCodes);
        }

        /// <summary>
        /// Copy the response and content headers from an httpResponse into the given dictionary.
        /// </summary>
        private void PopulateHeaders(HttpResponseMessage httpResponse, Dictionary<string, string> headers)
        {
            foreach (var header in httpResponse.Headers.Concat(httpResponse.Content.Headers))
            {
                var value = string.Join(", ", header.Value);

                // Use the indexer rather than Add so a header appearing in both the response
                // and content header collections doesn't throw on a duplicate key.
                headers[header.Key] = value;
            }
        }

        /// <summary>
        /// Determine whether an httpResponse should be considered successful, taking into
        /// account both the client-level and request-level additional successful status codes.
        /// </summary>
        private bool ResponseIsSuccessful(HttpResponseMessage httpResponse, IEnumerable<HttpStatusCode> successfulStatusCodes) =>
            httpResponse.IsSuccessStatusCode ||
            AdditionalSuccessfulStatusCodes.Concat(successfulStatusCodes).Any(x => x == httpResponse.StatusCode);

        /// <summary>
        /// Create a URL for the given request.
        /// </summary>
        private string CreateUrl(ISimpleRequest request)
        {
            var url = request.UrlOverride.HasValue() ? request.UrlOverride : CombineUrls(Host, request.Path);

            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var parameter in request.QueryStringParameters)
            {
                query[parameter.Key] = parameter.Value;
            }

            builder.Query = query.ToString();

            return builder.ToString();
        }

        /// <summary>
        /// Combine two urls, handling slashes.
        /// </summary>
        private string CombineUrls(string url1, string url2)
        {
            if (!url1.HasValue())
            {
                return url2;
            }

            if (!url2.HasValue())
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return $"{url1}/{url2}";
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClientProvider?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// The method used when logging a request.
    /// </summary>
    /// <param name="url">The URL the request is pointing to.</param>
    /// <param name="request">The request object to log.</param>
    public delegate void LogRequest(string url, ISimpleRequest request);

    /// <summary>
    /// The method used when logging a response.
    /// </summary>
    /// <param name="response">The response object to log.</param>
    public delegate void LogResponse(ISimpleResponse response);
}
