// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A set of extension methods to make working with the <see cref="HttpClient"/> easier.
/// </summary>
internal static class HttpClientExtensions
{
    /// <summary>
    /// Adds a given header to the clients default request headers if it isn't blank.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to modify.</param>
    /// <param name="headerName">The name of the header.</param>
    /// <param name="headerValue">The value of the header.</param>
    internal static void AddHeaderIfNotNullOrWhiteSpace(this HttpClient client, string headerName, string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(headerValue))
        {
            return;
        }

        _ = client.DefaultRequestHeaders.TryAddWithoutValidation(headerName, headerValue);
    }
}
