// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Models;
using System.Net;
using System.Net.Http.Headers;

namespace CommunityToolkit.Datasync.Client.Offline.Operations;

/// <summary>
/// The executable operation for a "DELETE" operation.
/// </summary>
/// <param name="operation">The operation to execute.</param>
internal class DeleteOperation(DatasyncOperation operation) : ExecutableOperation
{
    /// <summary>
    /// Performs the push operation, returning the result of the push operation.
    /// </summary>
    /// <param name="options">The options to use for communicating with the datasync service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the push operation (async).</returns>
    internal override async Task<ServiceResponse> ExecuteAsync(EntityDatasyncOptions options, CancellationToken cancellationToken = default)
    {
        Uri endpoint = MakeAbsoluteUri(options.HttpClient.BaseAddress, options.Endpoint, operation.ItemId);
        using HttpRequestMessage request = new(HttpMethod.Delete, endpoint);
        if (!string.IsNullOrEmpty(operation.EntityVersion))
        {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{operation.EntityVersion}\""));
        }

        using HttpResponseMessage response = await options.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        ServiceResponse serviceResponse = new(response);

        // #397 - if the response is 404 (Not Found) or 410 (Gone), we return a successful response
        //        since the item was deleted on the server.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            // Convert the response to a successful response
            serviceResponse.StatusCode = (int)HttpStatusCode.NoContent;
        }

        return serviceResponse;
    }
}
