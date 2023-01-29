using Newtonsoft.Json.Linq;
using SimpleHttpClient.Models;

namespace SimpleHttpClient.Tests.Integration
{
    public class SimpleClientTests
    {
        [Fact]
        public async void NonTyped_GetRequest_Succeeds()
        {
            var client = new SimpleClient("https://postman-echo.com/");

            var request = new Request("/get?param1=value1&param2=value2");

            var response = await client.MakeRequest(request);

            var responseJson = JObject.Parse(response.StringBody);

            Assert.Equal("value1", responseJson?["args"]?["param1"]?.ToString());
            Assert.Equal("value2", responseJson?["args"]?["param2"]?.ToString());
        }
    }
}