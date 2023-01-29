﻿using SimpleHttpClient.Models;
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

        /// <summary>
        /// Constructor
        /// </summary>
        public SimpleClient(string host = null, HttpClient httpClient = null, ISimpleHttpSerializer serializer = null)
        {
            // I'm pretty sure newing up an HttpClient isn't the best way to handle it
            // but apparently, there is no "best way": https://github.com/dotnet/aspnetcore/issues/28385#issuecomment-853766480
            // This is how RestSharp does it, so it's probably fine...
            this.httpClient = httpClient ?? new HttpClient();

            this.serializer = serializer ?? new SimpleHttpDefaultJsonSerializer();
            this.host = host;
        }

        /// <summary>
        /// Execute a request
        /// </summary>
        public async Task<IResponse> MakeRequest(IRequest request) =>
            await MakeRequestInternal(request, new Response(), AddRequestBody, AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Execute a request
        /// </summary>
        public async Task<IResponse> MakeRequest<T>(IRequest<T> request) =>
            await MakeRequestInternal(request, new Response(), AddRequestBody, AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Execute a request
        /// </summary>
        public async Task<IResponse<T>> MakeRequest<T>(IRequest request) =>
            await MakeRequestInternal(request, new Response<T>(), AddRequestBody, AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Execute a request
        /// </summary>
        public async Task<IResponse<R>> MakeRequest<T, R>(IRequest<T> request) =>
            await MakeRequestInternal(request, new Response<R>(), AddRequestBody, AddResponseBody).ConfigureAwait(false);

        /// <summary>
        /// Execute a request
        /// </summary>
        private async Task<R> MakeRequestInternal<T, R>(T request,
            R response,
            Action<HttpRequestMessage, T> addRequestBody,
            Func<HttpResponseMessage, R, Task> addResponseBody) where T : IRequest where R : IResponse
        {
            var httpRequest = CreateHttpRequest(request);
            addRequestBody(httpRequest, request);

            var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);

            PopulateResponse(httpResponse, response);
            await addResponseBody(httpResponse, response);

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
            if (string.IsNullOrEmpty(request.StringBody))
            {
                return;
            }

            httpRequest.Content = new StringContent(request.StringBody, request.ContentEncoding, request.ContentType);
        }

        /// <summary>
        /// Add a body to the request
        /// </summary>
        private void AddRequestBody<T>(HttpRequestMessage httpRequest, IRequest<T> request)
        {
            if (request.Body == null)
            {
                return;
            }

            httpRequest.Content = new StringContent(serializer.Serialize(request.Body), request.ContentEncoding, request.ContentType);
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

            foreach (var parameter in request.QueryStringParameters)
            {
                query[parameter.Key] = parameter.Value;
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
