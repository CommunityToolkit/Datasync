# Authentication

Most of the time, you will want to use bearer authentication so that you can use a JWT (Json Web Token) obtained from an OIDC server.  This is so prevalent that we provide an easy mechanism to add this to your application via a `GenericAuthenticationProvider`.  The authentication provider only requests tokens from your token retrieval method when required (when the provided token is close to expiring or has expired).

## Set up authorization on the datasync service

You must set up authentication and authorization on the datasync service first.  The authentication and authorization configuration is the same as any other ASP.NET Core application, so [follow the instructions](https://learn.microsoft.com/aspnet/core/security/) for your particular provider.

!!! tip
    The most common mechanism used is JWT Bearer Token authentication with an OAuth 2.0 or OIDC authorization server.

## Create a method to retrieve the token

You need to implement a method to retrieve the token.  Normally, this uses the library that is provided for the purpose.  For example:

* Microsoft logins use [Microsoft.Identity.Client](https://www.nuget.org/packages/Microsoft.Identity.Client).
* Other logins on MAUI may use [WebAuthenticator](https://learn.microsoft.com/dotnet/maui/platform-integration/communication/authentication)

Whatever mechanism you use, this must be set up first. If your application is unable to get a token, the authentication middleware cannot pass it onto the server.

## Add the GenericAuthenticationProvider to your client

The `GenericAuthenticationProvider` takes a function that retrieves the token.  For example:

```csharp
using CommunityToolkit.Datasync.Client.Authentication;

public async Task<AuthenticationToken> GetTokenAsync(CancellationToken cancellationToken = default)
{
  // Put the logic to retrieve the JWT here.

  DateTimeOffset expiresOn = expiry-date;
  return new AuthenticationToken() 
  {
    Token = "the JWT you need to pass to the service",
    UserId = "the user ID",
    DisplayName = "the display Name",
    ExpiresOn = expiresOn
  };
}
```

You can now create a GenericAuthenticationProvider:

```csharp
GenericAuthenticationProvider authProvider = new(GetTokenAsync);
```

### Build HttpClientOptions with the authentication provider

The authentication provider is a `DelegatingHandler`, so it belongs in the `HttpPipeline`:

```csharp
HttpClientOptions options = new() 
{
  HttpPipeline = [ authProvider ],
  Endpont = "https://myservice.azurewebsites.net"
};
```

You can then use this options structure when constructing a datasync client.

!!! tip
    It's normal to inject the authentication provider as a singleton in an MVVM scenario with dependency injection.

## Forcing a login request

Sometimes, you want to force a login request; for example, in response to a button click.  You can call `LoginAsync()` on the authentication provider to trigger a login sequence.  The token will then be used until it expires.

## Refresh token

Most providers allow you to request a "refresh token" that can be used to silently request an access token for use in accessing the datasync service. You can store and retrieve refresh tokens from local storage in your token retrieval method.  The `GenericAuthenticationProvider` does not natively handle refresh tokens for you.

## Other options

You can specify which header is used for authorization.  For example, Azure App Service Authentication and Authorization service uses the `X-ZUMO-AUTH` header to transmit the token.  This is easily set up:

```csharp
GenericAuthenticationProvider authProvider = new(GetTokenAsync, "X-ZUMO-AUTH");
```

Similarly, you can specify the authentication type for the authorization header (instead of Bearer):

```csharp
GenericAuthenticationProvider authProvider = new(GetTokenAsync, "Authorization", "Basic");
```

This gives you significant flexibility to build the authentication mechanism appropriate for your application.

By default, a new token is requested if the old token is expired or within 2 minutes of expiry.  You can adjust the amount of buffer time using the `RefreshBufferTimeSpan` property:

```csharp
GenericAuthenticationProvider authProvider = new(GetTokenAsync)
{
  RefreshBufferTimeSpan = TimeSpan.FromSeconds(30)
};
```
