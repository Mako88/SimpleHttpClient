using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System.Net;
using System.Text;

namespace SimpleHttpClient.Tests
{
    public class SimpleRequestTests
    {
        [Fact]
        public void PropertyDefaults_AreCorrect()
        {
            var request = new SimpleRequest("test");

            Assert.Equal("GET", request.Method.Method);
            Assert.Empty(request.QueryStringParameters);
            Assert.Empty(request.FormUrlEncodedParameters);
            Assert.Empty(request.AdditionalSuccessfulStatusCodes);
            Assert.NotNull(request.Id);
        }

        [Fact]
        public void StringBody_IsSetCorrectly()
        {
            var request = new SimpleRequest("test", body: "testing");

            Assert.Equal("testing", request.StringBody);
        }

        [Theory]
        [InlineData("http://localhost", "/somepath")]
        [InlineData("", "http://localhost/somepath")]
        [InlineData(null, "http://localhost/somepath")]
        [InlineData("http://localhost/somepath", "")]
        [InlineData("http://localhost/somepath", null)]
        public async Task UrlOverride_OverridesHostAndPath(string host, string path)
        {
            var client = new SimpleClient(host);

            var request = new SimpleRequest(path)
            {
                UrlOverride = "https://postman-echo.com/get?param1=value1&param2=value2",
            };

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }

        [Fact]
        public async Task SerializerOverride_OverridesClientSerializer()
        {
            var serializer = new Mock<ISimpleHttpSerializer>(MockBehavior.Loose);
            serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns((object obj) => JsonConvert.SerializeObject(obj));
            serializer.Setup(x => x.Deserialize<TestResponse>(It.IsAny<string>())).Returns((string str) => JsonConvert.DeserializeObject<TestResponse>(str)!);

            var serializer2 = new Mock<IUnusedSerializer>(MockBehavior.Loose);

            var client = new SimpleClient("https://postman-echo.com", serializer: serializer2.Object);

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                test = "test",
            });
            request.SerializerOverride = serializer.Object;

            var response = await client.MakeRequest<TestResponse>(request);

            serializer.Verify(x => x.Serialize(It.IsAny<object>()), Times.Once);
            serializer.Verify(x => x.Deserialize<It.IsAnyType>(It.IsAny<string>()), Times.Once);
            serializer2.Verify(x => x.Serialize(It.IsAny<object>()), Times.Never);
            serializer2.Verify(x => x.Deserialize<It.IsAnyType>(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AdditionalSuccessfullStatusCodes_AreUsedFor_IsSuccess()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/not/an/actual/path");
            request.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotFound);

            var response = await client.MakeRequest(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(response.IsSuccessful);
        }

        [Fact]
        public async Task TimeoutOverride_OverridesClientTimeout()
        {
            var client = new SimpleClient("http://localhost/some/nonexistant/path");
            client.Timeout = 3;

            var request = new SimpleRequest("/get");
            request.TimeoutOverride = 1;

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
                Thread.Sleep(2000);

                if (exception == null)
                {
                    exception = new InvalidOperationException();
                }
            });

            await Task.WhenAll(new[] { task1, task2 });

            Assert.IsType<TimeoutException>(exception);
        }

        [Fact]
        public async Task ContentEncoding_IsSet()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "testing",
            });
            request.ContentEncoding = Encoding.ASCII;

            var response = await client.MakeRequest(request);

            var body = JToken.Parse(response.StringBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("ascii", body["headers"]?["content-type"]?.ToString());
        }
    }

    public interface IUnusedSerializer : ISimpleHttpSerializer { }

    public class TestResponse
    {
        public string? Test { get; set; }
    }
}
