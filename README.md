# SimpleHttpClient
An easy-to-use .NET wrapper for HttpClient. No extension methods, and included interfaces allow for easy unit test mocking, and straightforward properties allows for easier debugging (the response body is available as a string, byte array, and/or a typed object).

## Installation
SimpleHttpClient is available on [NuGet](https://www.nuget.org/packages/SimpleHttpClient) and can installed through the NuGet Package Manager or by running
```
nuget install SimpleHttpClient
```

## Basic Usage
SimpleHttpClient is designed to be used with dependency injection in order to avoid pitfalls that come with using an `HttpClient`:

In `Program.cs`:
```csharp
// Register SimpleHttpClient with the ServiceCollection
await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSimpleHttpClient();
    })
    .Build()
    .RunAsync();
```

Then, in the class that will use the SimpleHttpClient:
```csharp
public class YourClientClass
{
    private readonly SimpleClient client;

    // Retrieve an ISimpleHttpClient through dependency injection
    public YourClientClass(ISimpleHttpClient client)
    {
        // Set the host on the retrieved client
        client.Host = "https://api.sampleapis.com";
    }

    public async Task<string> MakeRequest()
    {
        // Pass the path you want to call into the SimpleRequest constructor
        var request = new SimpleRequest("/coffee/hot");

        // Call MakeRequest on the client, passing your request, and get your response back
        var response = await client.MakeRequest(request);

        return response.StringBody;
    }
}
```

You can also call MakeRequest with a type to serialize to that type:
```csharp
public async Task<SomeResponseObject> MakeRequest()
{
    // Pass the path you want to call into the Request constructor
    var request = new SimpleRequest("/get");

    // Call MakeRequest on the client, passing your request, and get your response back
    var response = await client.MakeRequest<SomeResponseObject>(request);

    return response.Body;
}
```

If you're using SimpleHttpClient without dependency injection, you can just create an instance of SimpleClient:
```csharp
public class YourClientClass
{
    private readonly SimpleClient client;

    public YourClientClass()
    {
        // Pass the host you'll be calling into the SimpleClient constructor
        client = new SimpleClient("https://api.sampleapis.com");
    }

    public async Task<string> MakeRequest()
    {
        // Pass the path you want to call into the SimpleRequest constructor
        var request = new SimpleRequest("/coffee/hot");

        // Call MakeRequest on the client, passing your request, and get your response back
        var response = await client.MakeRequest(request);

        return response.StringBody;
    }
}
```
NOTE: Although `SimpleClient` implements `IDisposable`, it should NOT be created inside a `using` block, but instead should be disposed with the class that uses it.