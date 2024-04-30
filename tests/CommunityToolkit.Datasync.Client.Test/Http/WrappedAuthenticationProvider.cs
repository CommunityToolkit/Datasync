// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;

namespace CommunityToolkit.Datasync.Client.Test.Http;

/// <summary>
/// Wrap of the <see cref="GenericAuthenticationProvider"/> that provides public access to `SendAsync()`
/// </summary
[ExcludeFromCodeCoverage]
public class WrappedAuthenticationProvider(Func<Task<AuthenticationToken>> requestor, string header = "Authorization", string authType = null) : GenericAuthenticationProvider(requestor, header, authType)
{
    public Task<HttpResponseMessage> WrappedSendAsync(HttpRequestMessage request, CancellationToken token = default)
        => base.SendAsync(request, token);
}