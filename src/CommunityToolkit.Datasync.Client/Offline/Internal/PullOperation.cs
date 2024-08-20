// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// The executable operation for a "PULL" operation.
/// </summary>
/// <param name="entityType">The entity type being pulled.</param>
/// <param name="options">The JSON Serializer options to use.</param>
internal class PullOperation(Type entityType, JsonSerializerOptions options) : ExecutableOperation
{    
    /// <summary>
    /// Performs the get page operation, returning the result of the pull operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use for communicating with the datasync service.</param>
    /// <param name="endpoint">The fully-qualified URI to the table endpoint.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the push operation (async).</returns>
    internal override async Task<ServiceResponse> ExecuteAsync(HttpClient client, Uri endpoint, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return new ServiceResponse(response);
    }

    /// <summary>
    /// Performs the get page operation, returning the result of the pull operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use for communicating with the datasync service.</param>
    /// <param name="endpoint">The fully-qualified URI to the table endpoint.</param>
    /// <param name="queryString">The query string for the endpoint</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the push operation (async).</returns>
    internal async Task<ServiceResponse<Page<object>>> GetPageAsync(HttpClient client, Uri endpoint, string queryString, CancellationToken cancellationToken = default)
    {
        UriBuilder builder = new(MakeAbsoluteUri(client.BaseAddress, endpoint)) { Query = queryString };
        ServiceResponse response = await ExecuteAsync(client, builder.Uri, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessful && response.HasContent)
        {
            Type pageType = GetGenericPageType(entityType);
            return new ServiceResponse<Page<object>>(response)
            {
                Value = await JsonSerializer.DeserializeAsync(response.ContentStream, pageType, options, cancellationToken).ConfigureAwait(false) as Page<object>
            };
        }

        return new ServiceResponse<Page<object>>(response);
    }

    /// <summary>
    /// Creates a Type identifier for a <see cref="Page{TEntity}"/> type where TEntity is
    /// specified by a <see cref="Type"/>
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The type of the equivalent <see cref="Page{TEntity}"/> type.</returns>
    internal static Type GetGenericPageType(Type entityType)
        => typeof(Page<>).MakeGenericType([entityType]);
}
