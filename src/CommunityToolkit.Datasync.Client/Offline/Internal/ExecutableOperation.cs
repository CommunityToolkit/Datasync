// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// The abstract class that encapsulates all logic dealing with pushing an operation to the remote server.
/// </summary>
internal abstract class ExecutableOperation
{
    /// <summary>
    /// The JSON media type.
    /// </summary>
    internal MediaTypeHeaderValue JsonMediaType { get; } = MediaTypeHeaderValue.Parse("application/json");

    /// <summary>
    /// Converts a base address + relative/absolute URI into the appropriate URI for the datasync service.
    /// </summary>
    /// <param name="baseAddress">The base address from the client.</param>
    /// <param name="relativeOrAbsoluteUri">A relative or absolute URI</param>
    /// <returns></returns>
    internal static Uri MakeAbsoluteUri(Uri? baseAddress, Uri relativeOrAbsoluteUri)
    {
        if (relativeOrAbsoluteUri.IsAbsoluteUri)
        {
            return new Uri($"{relativeOrAbsoluteUri.ToString().TrimEnd('/')}/");
        }

        if (baseAddress != null)
        {
            if (baseAddress.IsAbsoluteUri)
            {
                return new Uri($"{new Uri(baseAddress, relativeOrAbsoluteUri).ToString().TrimEnd('/')}/");
            }
        }

        throw new UriFormatException("Invalid combination of baseAddress and relativeUri");
    }

    /// <summary>
    /// Performs the push operation, returning the result of the push operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use for communicating with the datasync service.</param>
    /// <param name="endpoint">The fully-qualified URI to the table endpoint.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the push operation (async).</returns>
    internal abstract Task<ServiceResponse> ExecuteAsync(HttpClient client, Uri endpoint, CancellationToken cancellationToken = default);

#pragma warning disable IDE0060 // Remove unused parameter - cancellationToken is kept for API consistency.
    /// <summary>
    /// Creates a new <see cref="ExecutableOperation"/> based on the operation model.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The <see cref="ExecutableOperation"/> to execute.</returns>
    /// <exception cref="DatasyncException">If the operation Kind is not supported for push.</exception>
    internal static Task<ExecutableOperation> CreateAsync(DatasyncOperation operation, CancellationToken cancellationToken = default) => operation.Kind switch
    {
        OperationKind.Add => Task.FromResult<ExecutableOperation>(new AddOperation(operation)),
        OperationKind.Delete => Task.FromResult<ExecutableOperation>(new DeleteOperation(operation)),
        OperationKind.Replace => Task.FromResult<ExecutableOperation>(new ReplaceOperation(operation)),
        _ => throw new DatasyncException($"Invalid operation kind '{operation.Kind}'"),
    };
#pragma warning restore IDE0060 // Remove unused parameter
}
