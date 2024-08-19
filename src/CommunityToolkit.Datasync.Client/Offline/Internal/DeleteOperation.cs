// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// The executable operation for a "DELETE" operation.
/// </summary>
/// <param name="operation">The operation to execute.</param>
internal class DeleteOperation(DatasyncOperation operation) : ExecutableOperation
{
    /// <summary>
    /// Performs the push operation, returning the result of the push operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use for communicating with the datasync service.</param>
    /// <param name="endpoint">The fully-qualified URI to the table endpoint.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the push operation (async).</returns>
    internal override async Task<ServiceResponse> ExecuteAsync(HttpClient client, Uri endpoint, CancellationToken cancellationToken = default)
    {
        endpoint = MakeAbsoluteUri(client.BaseAddress, endpoint);
        using HttpRequestMessage request = new(HttpMethod.Delete, new Uri(endpoint, operation.ItemId));
        if (!string.IsNullOrEmpty(operation.EntityVersion))
        {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{operation.EntityVersion}\""));
        }

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return new ServiceResponse(response);
    }
}
