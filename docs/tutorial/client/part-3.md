# Managing the HTTP pipeline

The [Datasync Community Toolkit][1] is a set of open-source libraries for building client-server application where the application data is available offline.  Unlike, for example, [Google Firebase][2] or [AWS AppSync][3] (which are two competitors in this space), the [Datasync Community Toolkit][1] allows you to connect to any database, use any authentication, and provides robust authorization rules.  You can also run the service anywhere - on your local machine, in a container, or on any cloud provider.  Each side of the application (client and server) is implemented using .NET - [ASP.NET Core Web APIs][4] on the server side, and any .NET client technology (including [Avalonia][5], [MAUI][6], [Uno Platform][7], [WinUI3][8], and [WPF][9]) on the client side.

In the [last tutorial](./part-2.md), I enhanced the [basic functionality](./part-1.md) by introducing authentication and authorization to both the service and client.  This was done by adjusting the HTTP pipeline to introduce a delegating handler called the `GenericAuthenticationProvider`.  When a HTTP request gets sent to the service, the request goes through a number of delegating handlers before being sent to the service.  These delegating handlers can each adjust the request.  Similarly, when the response comes back from the service, it goes through the delegating handlers in the opposite direction.

For example, let's consider the following configuration:

```csharp
  HttpClientOptions options = new()
  {
    Endpoint = new Uri("https://myserver/"),
    HttpPipeline = [
      new LoggingHandler(),
      new AuthenticationHandler()
    ],
    Timeout = TimeSpan.FromSeconds(120),
    UserAgent = "Enterprise/Datasync-myserver-service"
  };
  DatasyncServiceClient<TodoItem> serviceClient = new(options);
```

When you call `serviceClient.GetAsync("1234")`, the following will happen:

* The `serviceClient` will construct a `HttpRequestMessage`: GET /tables/todoitem/1234 and call the configured `HttpClient`.
* The `HttpClient` will then pass the request message to the root delegating handler (in this case, the `LoggingHandler`).
* Each delegating handler will pass the request message to the next delegating handler in the sequence.
* The final delegating handler (in this case, the `AuthenticationHandler`) will pass the request to the `HttpClientHandler`
* The `HttpClientHandler` will transmit the request to the service and await the response, encoding the response as a `HttpResponseMessage`.
* As the response is returned, it is passed up the chain of delegating handlers - `AuthenticationHandler`, then `LoggingHandler`.
* Finally, the root delegating handler passes the response message back to the `HttpClient`, which returns the message to the `serviceClient`.
* The `serviceClient` then decodes the response and passes it back to your code.

The point is - the order of those delegating handlers matters.  If, for example, you have the order as suggested above, the authentication header will not be logged because the logging handler won't see the authentication header in the request.

## The general form of a delegating handler

A delegating handler looks like this:

```csharp
public class MyDelegatingHandler : DelegatingHandler
{
  public MyDelegatingHandler() : base()
  {
  }

  public MyDelegatingHandler(HttpMessageHandler inner) : base(inner)
  {
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    // Adjust the request here

    HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

    // Adjust the response here

    return response;
  }
}
```

Let's take a look at two very common delegating handlers, starting with the logging handler.

### The logging handler

The logging handler is something I build into just about every single client application during development.  The purpose is to provide detailed HTTP level logging for every single request.  You can find this delegating handler in most of the samples as well.

```csharp
using System.Diagnostics;

public class LoggingHandler : DelegatingHandler
{
  public LoggingHandler() : base() { }
  public LoggingHandler(HttpMessageHandler inner) : base(inner) { }
  
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    Debug.WriteLine($"[HTTP] >>> {request.Message} {request.RequestUri}");
    await WriteContentAsync(request.Content, cancellationToken);

    HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

    Debug.WriteLine($"[HTTP] <<< {response.StatusCode} {response.ReasonPhrase}");
    await WriteContentAsync(response.Content, cancellationToken)
  }

  private static async Task WriteContentAsync(HttpContent? content, CancellationToken cancellationToken = default)
  {
    if (content is not null)
    {
      Debug.WriteLine(await content.ReadAsStringAsync(cancellationToken));
    }
  }
}
```

!!! warning "Do not use with HttpClient"
    While this code works for the datasync client, it likely does not work in all `HttpClient` cases.  This is because the content property can be a read-once stream.  In this case, your logging code would interfere with the application.

### Serilog logging

I've seen a lot of developers use [Serilog] for logging.  It's a solid framework, so there is no surprise that it is so popular.  Serilog has [a specific package][Serilog.HttpClient] for handling `HttpClient` logging.  After you have set up Serilog (probably in your App.xaml.cs file), you can do the following:

```csharp
using Serilog.HttpClient;

HttpClientOptions options = new()
{
  Endpoint = new Uri("https://myserver/"),
  HttpPipeline = [
    new LoggingDelegatingHandler(new RequestLoggingOptions()),
  ]
};
DatasyncServiceClient<TodoItem> serviceClient = new(options);
```

For more information, consult the documentation for [Serilog] and [Serilog.HttpClient].

### Adding an API Key

Another common request is to handle API keys.  Azure API Management, as an example, allows you to associate specific backend APIs with products (a collection of APIs) that are chosen with an API or subscription key.  This is done by passing an `Ocp-Apim-Subscription-Key` HTTP header with the request.  Here is the delegating handler:

```csharp
public class AzureApiManagementSubscriptionHandler : DelegatingHandler
{
  public AzureApiManagementSubscriptionHandler() : base() { }
  public AzureApiManagementSubscriptionHandler(HttpMessageHandler inner) : base(inner) { }

  public string ApiKey { get; set; } = string.Empty;

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    if (!string.IsNullOrWhiteSpace(ApiKey))
    {
      request.Headers.Add("Ocp-Apim-Subscription-Key", ApiKey);
    }

    return base.SendAsync(request, cancellationToken);
  }
}
```

!!! warning "Do not use API keys for authentication"
    API keys are easily retrieved from client applications and not suitable to authenticate a user or client application.

You can now use the following client options to route the request to the correct product within Azure API Management:

```csharp
  string apiKey = GetApiKeyFromConfiguration();
  HttpClientOptions options = new()
  {
    Endpoint = new Uri("https://myserver/"),
    HttpPipeline = [
      new LoggingHandler(),
      new AzureApiManagementSubscriptionKey(apiKey),
      new GenericAuthenticationProvider(GetAuthenticationTokenAsync)
    ]
  };
  DatasyncServiceClient<TodoItem> serviceClient = new(options);
```

Note that we log the request, then add the API key and Authorization headers.  In this way, privileged information (such as the authorization token) is not logged.

## Wrapping up

Adding delegating handlers to your client HTTP pipeline allows you to integrate any functionality you want on a per-request basis.  This includes any authentication scheme, API keys, and request/response logging.

In the next tutorial, we move onto offline operations.

<!-- Links -->
[1]: https://github.com/CommunityToolkit/Datasync
[2]: https://firebase.google.com/
[3]: https://docs.aws.amazon.com/appsync/latest/devguide/what-is-appsync.html
[4]: https://learn.microsoft.com/training/modules/build-web-api-aspnet-core/
[5]: https://avaloniaui.net/
[6]: https://dotnet.microsoft.com/apps/maui
[7]: https://platform.uno/
[8]: https://learn.microsoft.com/windows/apps/winui/winui3/
[9]: https://wpf-tutorial.com/
[Serilog]: https://serilog.net/
[Serilog.HttpClient]: https://github.com/alirezavafi/serilog-httpclient