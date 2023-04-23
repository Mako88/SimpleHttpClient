using Moq;
using Newtonsoft.Json.Linq;
using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SimpleHttpClient.Tests
{
    public class SimpleClientTests
    {
        [Fact]
        public void PropertyDefaults_AreCorrect()
        {
            var client = new SimpleClient();

            Assert.NotNull(client.HttpClient);
            Assert.IsType<SimpleHttpDefaultJsonSerializer>(client.Serializer);
            Assert.NotNull(client.Serializer);
            Assert.Null(client.Logger);
            Assert.Empty(client.AdditionalSuccessfulStatusCodes);
            Assert.Empty(client.DefaultHeaders);
            Assert.Equal(client.DefaultHeaders.Comparer.GetHashCode("test"), client.DefaultHeaders.Comparer.GetHashCode("TEST"));
            Assert.Equal(30, client.Timeout);
        }

        [Theory]
        [InlineData("https://postman-echo.com", "get")]
        [InlineData("https://postman-echo.com/", "get")]
        [InlineData("https://postman-echo.com", "/get")]
        [InlineData("https://postman-echo.com/", "/get")]
        [InlineData(null, "https://postman-echo.com/get")]
        [InlineData("", "https://postman-echo.com/get")]
        [InlineData("https://postman-echo.com/get", "")]
        [InlineData("https://postman-echo.com/get", null)]
        public async Task AllUrlVariations_Succeed(string host, string path)
        {
            var client = new SimpleClient(host);

            var request = new SimpleRequest(path);

            var response = await client.MakeRequest(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        [InlineData("put")]
        [InlineData("patch")]
        [InlineData("delete")]
        public async Task CommonHttpMethods_Succeed(string method)
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest($"/{method}", GetHttpMethod(method));

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ParallelRequests_Succeed()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Run(async () =>
                {
                    var value1 = Guid.NewGuid().ToString();
                    var value2 = Guid.NewGuid().ToString();

                    var request = new SimpleRequest($"/get?param1={value1}&param2={value2}");

                    var response = await client.MakeRequest<PostmanEchoResponse>(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(value1, response.Body?.Args?.Param1);
                    Assert.Equal(value2, response.Body?.Args?.Param2);
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task TypedResponse_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get?param1=value1&param2=value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task UntypedResponse_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get?param1=value1&param2=value2");

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task Request_WithUrlQueryString_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get?param1=value1&param2=value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task Request_WithQueryStringParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");
            request.QueryStringParameters.Add("param1", "value1");
            request.QueryStringParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task Request_WithUrlQueryString_AndQueryStringParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get?param1=value1");
            request.QueryStringParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task QueryStringParameter_Overwrites_UrlQueryString()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get?param1=willbeoverwritten&param2=alsooverwritten");
            request.QueryStringParameters.Add("param1", "value1");
            request.QueryStringParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Args?.Param1);
            Assert.Equal("value2", response.Body?.Args?.Param2);
        }

        [Fact]
        public async Task Request_WithUrlFormEncodedParameters_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post);

            request.FormUrlEncodedParameters.Add("param1", "value1");
            request.FormUrlEncodedParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Form?.Param1);
            Assert.Equal("value2", response.Body?.Form?.Param2);
            Assert.Equal(Constants.FormUrlEncodedContentType, response.Body?.Headers?["content-type"]?.ToString());
        }

        [Fact]
        public async Task Request_WithBody_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "value1",
                param2 = "value2",
            });

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Data?.Param1);
            Assert.Equal("value2", response.Body?.Data?.Param2);
        }

        [Fact]
        public async Task Request_WithUrlFormEncodedParameters_Overwrites_CustomContentType()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post);

            request.ContentType = "application/json";

            request.FormUrlEncodedParameters.Add("param1", "value1");
            request.FormUrlEncodedParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Form?.Param1);
            Assert.Equal("value2", response.Body?.Form?.Param2);
            Assert.Equal(Constants.FormUrlEncodedContentType, response.Body?.Headers?["content-type"]?.ToString());
        }

        [Fact]
        public async Task UrlFormEncodedParamaters_OverwritesStringBody()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post);
            request.StringBody = "{ \"param1\": \"willbeoverwritten\", \"param2\": \"alsooverwritten\"}";

            request.FormUrlEncodedParameters.Add("param1", "value1");
            request.FormUrlEncodedParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Form?.Param1);
            Assert.Equal("value2", response.Body?.Form?.Param2);
            Assert.NotEqual("willbeoverwritten", response.Body?.Data?.Param1);
            Assert.NotEqual("alsooverwritten", response.Body?.Data?.Param2);
        }

        [Fact]
        public async Task UrlFormEncodedParamaters_OverwritesBody()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "willbeoverwritten",
                param2 = "alsooverwritten",
            });

            request.FormUrlEncodedParameters.Add("param1", "value1");
            request.FormUrlEncodedParameters.Add("param2", "value2");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Form?.Param1);
            Assert.Equal("value2", response.Body?.Form?.Param2);
            Assert.NotEqual("willbeoverwritten", response.Body?.Data?.Param1);
            Assert.NotEqual("alsooverwritten", response.Body?.Data?.Param2);
        }

        [Fact]
        public async Task StringBody_OverwritesBody()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "willbeoverwritten",
                param2 = "alsooverwritten",
            });

            request.StringBody = "{ \"param1\": \"value1\", \"param2\": \"value2\"}";

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("value1", response.Body?.Data?.Param1);
            Assert.Equal("value2", response.Body?.Data?.Param2);
        }

        [Fact]
        public async Task DefaultUserAgent_IsSet()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Constants.DefaultUserAgent, response.Body?.Headers?["user-agent"]?.ToString());
        }

        [Fact]
        public async Task CustomUserAgent_Overrides_DefaultUserAgent()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");

            request.Headers["User-Agent"] = "CustomUserAgent";

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("CustomUserAgent", response.Body?.Headers?["user-agent"]?.ToString());
        }

        [Fact]
        public async Task ContentTypeHeader_IsSetOnBodyContent()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "testing",
            });
            request.Headers["Content-Type"] = "text/json";

            var response = await client.MakeRequest(request);

            var body = JToken.Parse(response.StringBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/json", request.ContentType);
            Assert.Contains("text/json", body["headers"]?["content-type"]?.ToString());
        }

        [Fact]
        public async Task ContentTypeHeader_DoesNotOverride_RequestContentType()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "testing",
            });
            request.Headers["Content-Type"] = "text/somethingelse";
            request.ContentType = "text/json";

            var response = await client.MakeRequest(request);

            var body = JToken.Parse(response.StringBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/json", request.ContentType);
            Assert.Contains("text/json", body["headers"]?["content-type"]?.ToString());
        }

        [Fact]
        public async Task DefaultHeaders_AreSent()
        {
            var client = new SimpleClient("https://postman-echo.com");
            client.DefaultHeaders["test1"] = "testValue1";
            client.DefaultHeaders["test2"] = "testValue2";

            var request = new SimpleRequest("/get");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("testValue1", response.Body?.Headers?["test1"]?.ToString());
            Assert.Equal("testValue2", response.Body?.Headers?["test2"]?.ToString());
        }

        [Fact]
        public async Task RequestHeaders_AreSent()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");
            request.Headers["test1"] = "testValue1";
            request.Headers["test2"] = "testValue2";

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("testValue1", response.Body?.Headers?["test1"]?.ToString());
            Assert.Equal("testValue2", response.Body?.Headers?["test2"]?.ToString());
        }

        [Fact]
        public async Task StringBody_IsSet_ToSerializedBody()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "value1",
                param2 = "value2",
            });

            await client.MakeRequest<PostmanEchoResponse>(request);

            var body = JToken.Parse(request.StringBody);

            Assert.Equal("value1", body["param1"]?.ToString());
            Assert.Equal("value2", body["param2"]?.ToString());
        }

        [Fact]
        public async Task LogMethods_AreCalled()
        {
            var logger = new Mock<ISimpleHttpLogger>(MockBehavior.Loose);

            var client = new SimpleClient("https://postman-echo.com")
            {
                Logger = logger.Object,
            };

            var request = new SimpleRequest("/get");

            await client.MakeRequest(request);

            logger.Verify(x => x.LogRequest("https://postman-echo.com/get", request), Times.Once);
            logger.Verify(x => x.LogResponse(It.Is<ISimpleResponse>(y => y.Id == request.Id)));
        }

        [Fact]
        public async Task Timeout_Throws_TimeoutException()
        {
            var client = new SimpleClient("https://postman-echo.com");
            client.Timeout = 0;

            var request = new SimpleRequest("/get?param1=value1&param2=value2");

            await Assert.ThrowsAsync<TimeoutException>(async () => await client.MakeRequest<PostmanEchoResponse>(request));
        }

        [Fact]
        public async Task Timeout_WaitsTheCorrectAmountOfTime()
        {
            var client = new SimpleClient("http://localhost/some/nonexistant/path");
            client.Timeout = 2;

            var request = new SimpleRequest("/get");

            Exception? exception = null;

            var task1 = Task.Run(async () =>
            {
                try
                {
                    await client.MakeRequest(request);
                }
                catch (Exception ex)
                {
                    if (exception == null)
                    {
                        exception = ex;
                    }
                }
            });

            var task2 = Task.Run(() =>
            {
                Thread.Sleep(3000);

                if (exception == null)
                {
                    exception = new InvalidOperationException();
                }
            });

            await Task.WhenAll(new[] { task1, task2 });

            Assert.IsType<TimeoutException>(exception);
        }

        [Fact]
        public void GetUrl_ReturnsCorrectUrl()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");

            var url = client.GetUrl(request);

            Assert.Equal("https://postman-echo.com:443/get", url);
        }

        [Fact]
        public async Task AdditionalSuccessfullStatusCodes_AreConcatenatedFor_IsSuccess()
        {
            var server = WireMockServer.Start();

            server.Given(Request.Create().WithPath("/test").UsingGet())
                .InScenario("test")
                .WillSetStateTo("test2")
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NotAcceptable));

            server.Given(Request.Create().WithPath("/test").UsingGet())
                .InScenario("test")
                .WhenStateIs("test2")
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NotFound));

            var client = new SimpleClient(server.Url);
            client.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotAcceptable);

            var request = new SimpleRequest("/test");
            request.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotFound);

            var response = await client.MakeRequest(request);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            Assert.True(response.IsSuccessful);

            var response2 = await client.MakeRequest(request);

            Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
            Assert.True(response2.IsSuccessful);

            server.Stop();
        }

        [Fact]
        public async Task ResponseId_IsSet_ToRequestId()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");

            var response = await client.MakeRequest(request);

            Assert.NotNull(request.Id);
            Assert.Equal(request.Id, response.Id);
        }

        private HttpMethod GetHttpMethod(string method) => method switch
        {
            "get" => HttpMethod.Get,
            "post" => HttpMethod.Post,
            "put" => HttpMethod.Put,
            "patch" => HttpMethod.Patch,
            "delete" => HttpMethod.Delete,
            _ => HttpMethod.Get,
        };
    }

    public class PostmanEchoResponse
    {
        public Args? Args { get; set; }

        public Args? Form { get; set; }

        public Args? Data { get; set; }

        public JToken? Headers { get; set; }
    }

    public class Args
    {
        public string? Param1 { get; set; }
        public string? Param2 { get; set; }
    }
}