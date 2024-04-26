// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A generic authentication provider that gets a JWT token from the specified
/// action and puts it in the Authorization header.  The JWT is kept around until
/// about 2-3 minutes before it expires, at which point a new one is requested.
/// </summary>
public class GenericAuthenticationProvider : AuthenticationProvider
{
    private TimeSpan bufferPeriod = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Creates a new <see cref="GenericAuthenticationProvider"/> by specifying a function for the token requester.
    /// </summary>
    /// <param name="asyncTokenRequestor">The token requester</param>
    /// <param name="headerName">The name of the header</param>
    /// <param name="authenticationType">The authentication type (if specified)</param>
    public GenericAuthenticationProvider(Func<Task<AuthenticationToken?>> asyncTokenRequestor, string headerName = "Authorization", string? authenticationType = null)
    {
        Ensure.That(headerName.Trim(), nameof(headerName)).IsNotNullOrWhiteSpace();
        TokenRequestorAsync = asyncTokenRequestor ?? throw new ArgumentNullException(nameof(asyncTokenRequestor));

        if (headerName.Equals("authorization", StringComparison.InvariantCultureIgnoreCase))
        {
            authenticationType ??= "Bearer";
        }

        if (authenticationType != null && string.IsNullOrWhiteSpace(authenticationType))
        {
            throw new ArgumentException($"{nameof(authenticationType)} must be specified (or null if not Authorization header)", nameof(authenticationType));
        }

        HeaderName = headerName;
        AuthenticationType = authenticationType;
    }

    /// <summary>
    /// The function used to request the token.
    /// </summary>
    internal Func<Task<AuthenticationToken?>> TokenRequestorAsync { get; set; }

    /// <summary>
    /// The header name to use for authentication
    /// </summary>
    internal string HeaderName { get; }

    /// <summary>
    /// The authentication type (normally Bearer)
    /// </summary>
    internal string? AuthenticationType { get; }

    /// <summary>
    /// The current authentication token
    /// </summary>
    internal AuthenticationToken? Current { get; set; }

    /// <summary>
    /// The amount of time prior to expiry that we refresh the token
    /// </summary>
    public TimeSpan RefreshBufferTimeSpan
    {
        get => this.bufferPeriod;
        set
        {
            Ensure.That(value, nameof(RefreshBufferTimeSpan)).IsGt(TimeSpan.FromMilliseconds(1000));
            this.bufferPeriod = value;
        }
    }

    /// <summary>
    /// Initiate a login request out of band of the pipeline.  This can be used to initiate the login process via a button.
    /// </summary>
    /// <returns>An async task that resolves when the login is complete</returns>
    public override Task LoginAsync() => GetTokenAsync(true);

    /// <summary>
    /// Gets a valid authentication token
    /// </summary>
    /// <param name="force">If true, forces re-acquisition of the authentication token</param>
    /// <returns>The authentication token (asynchronously)</returns>
    public async Task<string?> GetTokenAsync(bool force = false)
    {
        if (force || IsExpired(Current))
        {
            Current = await TokenRequestorAsync.Invoke().ConfigureAwait(false);
            IsLoggedIn = !IsExpired(Current);
            UserId = !IsLoggedIn ? null : Current.Value.UserId;
            DisplayName = !IsLoggedIn ? null : Current.Value.DisplayName;
        }

        if (IsExpired(Current))
        {
            System.Diagnostics.Debug.WriteLine($"GenericAuthenticationProvider:: Current is expired - token = {Current}");
            return null;
        }

        return Current!.Value.Token;
    }

    /// <summary>
    /// Determines if the token is valid and unexpired.
    /// </summary>
    /// <param name="token">The token</param>
    /// <returns>true if the token is valid.</returns>
    internal bool IsExpired(AuthenticationToken? token)
    {
        try
        {
            if (!token.HasValue)
            {
                return true;
            }

            return DateTimeOffset.Now >= token.Value.ExpiresOn.Subtract(RefreshBufferTimeSpan);
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// The delegating handler for this request - injects the authorization header into the request.
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/></param>
    /// <returns>The response (asynchronously)</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        if (request.Headers.Contains(HeaderName))
        {
            _ = request.Headers.Remove(HeaderName);
        }

        string? token = await GetTokenAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Add(HeaderName, AuthenticationType != null ? $"{AuthenticationType} {token}" : token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
