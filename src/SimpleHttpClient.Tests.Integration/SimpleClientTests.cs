using Newtonsoft.Json.Linq;
using SimpleHttpClient.Models;

namespace SimpleHttpClient.Tests.Integration
{
    public class SimpleClientTests
    {
        [Theory]
        [InlineData("https://postman-echo.com", "get?param1=value1&param2=value2")]
        [InlineData("https://postman-echo.com/", "get?param1=value1&param2=value2")]
        [InlineData("https://postman-echo.com", "/get?param1=value1&param2=value2")]
        [InlineData("https://postman-echo.com/", "/get?param1=value1&param2=value2")]
        [InlineData(null, "https://postman-echo.com/get?param1=value1&param2=value2")]
        [InlineData("", "https://postman-echo.com/get?param1=value1&param2=value2")]
        [InlineData("https://postman-echo.com/get?param1=value1&param2=value2", "")]
        [InlineData("https://postman-echo.com/get?param1=value1&param2=value2", null)]
        public async Task AllUrlVariations_Succeed(string host, string path)
        {
            var client = new SimpleClient(host);

            var request = new Request(path);

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Theory]
        [InlineData("http://localhost", "/somepath", "https://postman-echo.com/get?param1=value1&param2=value2")]
        [InlineData("", "http://localhost/somepath", "https://postman-echo.com/get?param1=value1&param2=value2")]
        [InlineData(null, "http://localhost/somepath", "https://postman-echo.com/get?param1=value1&param2=value2")]
        [InlineData("http://localhost/somepath", "", "https://postman-echo.com/get?param1=value1&param2=value2")]
        [InlineData("http://localhost/somepath", null, "https://postman-echo.com/get?param1=value1&param2=value2")]
        public async Task OverrideUrl_OverridesHostAndPath(string host, string path, string overrideUrl)
        {
            var client = new SimpleClient(host);

            var request = new Request(path)
            {
                OverrideUrl = overrideUrl,
            };

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task NonTyped_GetRequest_WithUrlQueryString_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/get?param1=value1&param2=value2");

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task Typed_GetRequest_WithUrlQueryString_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/get?param1=value1&param2=value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task NonTyped_GetRequest_WithQueryStringParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/get");
            request.QueryStringParameters.Add("param1", "value1");
            request.QueryStringParameters.Add("param2", "value2");

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task Typed_GetRequest_WithQueryStringParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/get");
            request.QueryStringParameters.Add("param1", "value1");
            request.QueryStringParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Theory]
        [InlineData("https://postman-echo.com", "/get?param1=value1", false)]
        [InlineData("https://postman-echo.com", "/get?param1=willbeoverwritten", true)]
        public async Task NonTyped_GetRequest_WithUrlQueryString_AndQueryStringParameters_Succeeds(string host, string path, bool shouldIncludeParam1)
        {
            var client = new SimpleClient(host);

            var request = new Request(path);
            request.QueryStringParameters.Add("param2", "value2");

            if (shouldIncludeParam1)
            {
                request.QueryStringParameters.Add("param1", "value1");
            }

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Theory]
        [InlineData("https://postman-echo.com", "/get?param1=value1", false)]
        [InlineData("https://postman-echo.com", "/get?param1=willbeoverwritten", true)]
        public async Task Typed_GetRequest_WithUrlQueryString_AndQueryStringParameters_Succeeds(string host, string path, bool shouldIncludeParam1)
        {
            var client = new SimpleClient(host);

            var request = new Request(path);
            request.QueryStringParameters.Add("param2", "value2");

            if (shouldIncludeParam1)
            {
                request.QueryStringParameters.Add("param1", "value1");
            }

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task NonTyped_PostRequest_WithUrlQueryString_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/post?param1=value1&param2=value2", HttpMethod.Post);

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task Typed_PostRequest_WithUrlQueryString_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/post?param1=value1&param2=value2", HttpMethod.Post);

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task NonTyped_PostRequest_WithUrlFormEncodedParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/post", HttpMethod.Post);

            request.FormUrlEncodedParameters.Add("param1", "value1");
            request.FormUrlEncodedParameters.Add("param2", "value2");

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["form"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["form"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task Typed_PostRequest_WithUrlFormEncodedParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new Request("/post", HttpMethod.Post);

            request.FormUrlEncodedParameters.Add("param1", "value1");
            request.FormUrlEncodedParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal("value1", response.Body?.Form?.Param1);
            Assert.Equal("value2", response.Body?.Form?.Param2);
        }
    }

    public class PostmanEchoResponse
    {
        public Args? Args { get; set; }

        public Args? Form { get; set; }
    }

    public class Args
    {
        public string? Param1 { get; set; }
        public string? Param2 { get; set; }
    }
}