# SimpleHttpClient
An easy-to-use .NET wrapper for HttpClient. No extension methods, and included interfaces allow for easy unit test mocking, and straightforward properties allows for easier debugging (the response body is available as a string and/or a typed object).

## Usage
    public class YourClass
    {
        private readonly SimpleClient client;

        public YourClass()
        {
            // Pass the host you'll be calling into the SimpleClient constructor
            client = new SimpleClient("https://postman-echo.com");
        }

        public async Task<string> MakeRequest()
        {
            // Pass the path you want to call into the SimpleRequest constructor
            var request = new SimpleRequest("/get");

            // Call MakeRequest on the client, passing your request, and get your response back
            var response = await client.MakeRequest(request);
            
            return response.StringBody;
        }
    }

You can also call MakeRequest with a type to serialize to that type:

    public async Task<SomeResponseObject> MakeRequest()
    {
        // Pass the path you want to call into the Request constructor
        var request = new SimpleRequest("/get");

        // Call MakeRequest on the client, passing your request, and get your response back
        var response = await client.MakeRequest<SomeResponseObject>(request);

        return response.Body;
    }
