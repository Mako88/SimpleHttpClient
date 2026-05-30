# SimpleHttpClient
[![CI](https://github.com/Mako88/SimpleHttpClient/actions/workflows/ci.yml/badge.svg)](https://github.com/Mako88/SimpleHttpClient/actions/workflows/ci.yml)

An easy-to-use .NET wrapper for `HttpClient`. No extension methods, included interfaces allow for easy unit test mocking, and straightforward properties allow for easier debugging (the response body is available as a string, byte array, and/or a typed object).

## Contents
- [Installation](#installation)
- [Basic Usage](#basic-usage)
  - [With Dependency Injection](#with-dependency-injection)
  - [Without Dependency Injection](#without-dependency-injection)
  - [Typed Responses](#typed-responses)
- [Requests](#requests)
  - [Query String Parameters](#query-string-parameters)
  - [Form URL Encoded Parameters](#form-url-encoded-parameters)
  - [Headers](#headers)
  - [Request Bodies](#request-bodies)
- [Streaming Responses](#streaming-responses)
- [Configuration](#configuration)
  - [Timeouts](#timeouts)
  - [Additional Successful Status Codes](#additional-successful-status-codes)
  - [Custom Serializers](#custom-serializers)
  - [Logging](#logging)

## Installation
SimpleHttpClient is available on [NuGet](https://www.nuget.org/packages/SimpleHttpClient) and can installed through the NuGet Package Manager or by running
```
nuget install SimpleHttpClient
```
The package targets `netstandard2.0` (for .NET Framework and older runtimes) and `net8.0`. On modern runtimes it uses `SocketsHttpHandler` with a pooled connection lifetime to keep DNS fresh; on `netstandard2.0` it periodically rotates the underlying `HttpClient` to achieve the same.

## Basic Usage

### With Dependency Injection
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

Then, inject `ISimpleClientFactory` and create a client with the host you want to call. This is the
preferred approach: each consumer gets its own client, so there's no shared, mutable `Host` to
collide over.
```csharp
public class YourClientClass
{
    private readonly ISimpleClient client;

    // Retrieve an ISimpleClientFactory through dependency injection
    public YourClientClass(ISimpleClientFactory clientFactory)
    {
        // Create a client for the host you'll be calling
        client = clientFactory.CreateClient("https://api.sampleapis.com");
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

You can also inject `ISimpleClient` directly and set its `Host` (it's registered as transient, so
each consumer gets its own instance):
```csharp
public YourClientClass(ISimpleClient client)
{
    client.Host = "https://api.sampleapis.com";
}
```

### Without Dependency Injection
If you're using SimpleHttpClient without dependency injection, you can just create an instance of `SimpleClient`:
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

### Typed Responses
You can also call `MakeRequest` with a type to deserialize the response body into that type:
```csharp
public async Task<SomeResponseObject> MakeRequest()
{
    // Pass the path you want to call into the SimpleRequest constructor
    var request = new SimpleRequest("/get");

    // Call MakeRequest<T> on the client, passing your request, and get your response back
    var response = await client.MakeRequest<SomeResponseObject>(request);

    return response.Body;
}
```
The untyped `StringBody` and `ByteBody` are still available on a typed response. If deserialization fails, `response.Body` will be `null` and the thrown exception is available on `response.SerializationException`.

## Requests
A `SimpleRequest` defaults to a `GET`. Pass an `HttpMethod` to change it:
```csharp
var request = new SimpleRequest("/post", HttpMethod.Post);
```

### Query String Parameters
Query string parameters can be added directly to the path, via the `QueryStringParameters` dictionary, or both. Values in `QueryStringParameters` take precedence over duplicates in the path:
```csharp
var request = new SimpleRequest("/get?param1=value1");
request.QueryStringParameters.Add("param2", "value2");
```

### Form URL Encoded Parameters
Add `application/x-www-form-urlencoded` parameters via the `FormUrlEncodedParameters` dictionary. When present, these take precedence over any body set on the request:
```csharp
var request = new SimpleRequest("/post", HttpMethod.Post);
request.FormUrlEncodedParameters.Add("param1", "value1");
request.FormUrlEncodedParameters.Add("param2", "value2");
```

### Headers
Headers set on the request are merged with the client's `DefaultHeaders` (request headers win on conflicts):
```csharp
// Sent with every request made by this client
client.DefaultHeaders["Authorization"] = "Bearer <token>";

// Sent with just this request
var request = new SimpleRequest("/get");
request.Headers["X-Custom-Header"] = "value";
```

### Request Bodies
Pass an object as the body and it will be serialized using the client's serializer (JSON by default):
```csharp
var request = new SimpleRequest("/post", HttpMethod.Post, new
{
    param1 = "value1",
    param2 = "value2",
});
```
Alternatively, set `request.StringBody` to send a pre-serialized string body. You can control the content type and encoding via `request.ContentType` and `request.ContentEncoding`.

After a request is sent, the request reflects what was actually sent: for an object `Body`, `request.StringBody` holds the serialized payload, and `request.ContentType` holds the resolved content type — handy for logging and debugging. An object `Body` is the source of truth and is re-serialized on every send (so changing `Body` and re-sending the same request sends the new value); a string body is sent as-is.

> **Note:** On .NET Framework, sending a `GET` request with a body isn't supported — its `HttpClient` is backed by `HttpWebRequest`, which disallows it — and SimpleClient surfaces this as a `NotSupportedException` with an explanatory message. This works fine on modern runtimes (`net8.0`+); if you need to target .NET Framework, send the body with a `POST`/`PUT`/etc. instead.

## Streaming Responses
For responses you want to consume as they arrive — for example Server-Sent Events (SSE) or large downloads — use `MakeStreamRequest`. Unlike `MakeRequest`, it does **not** buffer the body into memory; it returns the live network stream as soon as the response headers are available.

```csharp
var request = new SimpleRequest("/stream");

// The response holds the connection open, so dispose it when you're done (a using block is ideal).
using var response = await client.MakeStreamRequest(request);

if (!response.IsSuccessful)
{
    // response.StatusCode and response.Headers are available immediately
}

// response.Body is the raw, unbuffered network stream
using var reader = new StreamReader(response.Body);

string line;
while ((line = await reader.ReadLineAsync()) != null)
{
    // Process each line as it arrives
    Console.WriteLine(line);
}
```

A few things to keep in mind:
- **Dispose the response.** The underlying `HttpResponseMessage` and connection are held open until you dispose the returned `ISimpleStreamResponse`. A `using` block is the simplest way to guarantee this.
- **`MakeStreamRequest` accepts a `CancellationToken`.** Pass one to cancel sending the request, waiting for the headers, and reading the stream (e.g. when a user aborts mid-stream). The token is observed by reads too, even through a `StreamReader` that gives you no place to pass it — so the `await reader.ReadLineAsync()` loop above stops promptly when the token fires. Async reads honor it even mid-read; synchronous reads observe it between reads, so to abort a synchronous read already blocked on the socket, dispose the response. A caller-requested cancellation surfaces as an `OperationCanceledException`; a timeout still surfaces as a `TimeoutException`.
- **The body is yours to frame.** `SimpleStreamResponse.Body` is a plain `Stream`, leaving any protocol-specific framing (such as SSE `event:`/`data:` parsing) to you.

## Configuration

### Timeouts
The client `Timeout` defaults to 30 seconds and can be overridden per-request. Set the value to `-1` to disable the timeout. A request that exceeds its timeout throws a `TimeoutException`:
```csharp
client.Timeout = 60;            // 60 seconds for all requests on this client

var request = new SimpleRequest("/slow");
request.TimeoutOverride = 120;  // 120 seconds for just this request
```
For streaming requests, the timeout applies to receiving the response headers — not to how long you spend reading the stream.

### Additional Successful Status Codes
By default, `IsSuccessful` is `true` for any 2xx status code. You can mark additional status codes as successful on the client (applies to all requests) and/or per-request:
```csharp
client.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotFound);

var request = new SimpleRequest("/get");
request.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotAcceptable);
```

### Custom Serializers
SimpleHttpClient ships with JSON (default) and XML serializers, and uses the serializer to both serialize request bodies and deserialize typed responses. Set one on the client, or override it per-request:
```csharp
client.Serializer = new SimpleHttpDefaultXmlSerializer();

var request = new SimpleRequest("/get");
request.SerializerOverride = new SimpleHttpDefaultJsonSerializer();
```
You can supply your own serializer by implementing `ISimpleHttpSerializer`.

#### JSON serialization
The default JSON serializer (`SimpleHttpDefaultJsonSerializer`) is backed by `System.Text.Json`. It serializes with camelCase names, omits null values, writes indented output, and deserializes case-insensitively. The equivalent `SimpleHttpSystemTextJsonSerializer` is also available for callers who reference it explicitly.

> **Upgrading from v4?** As of **v5.0.0** the default serializer moved from `Newtonsoft.Json` to `System.Text.Json` and the `Newtonsoft.Json` dependency was removed. `System.Text.Json` is stricter, so watch for: types deserialized via a non-public parameterless constructor (add a public constructor or a `[JsonConstructor]`), and fields whose JSON shape varies (e.g. sometimes a string, sometimes an object) — these threw nothing under Newtonsoft but will under `System.Text.Json`. If you need the old behavior, implement `ISimpleHttpSerializer` with your own `Newtonsoft.Json` serializer and set it on the client.

### Logging
You can log requests and responses by setting the `LogRequest` and `LogResponse` delegates (called immediately before a request is sent and immediately after a response is received), or by providing an `ISimpleHttpLogger`:
```csharp
client.LogRequest = (url, request) => Console.WriteLine($"--> {request.Method} {url}");
client.LogResponse = (response) => Console.WriteLine($"<-- {response.StatusCode}");
```
