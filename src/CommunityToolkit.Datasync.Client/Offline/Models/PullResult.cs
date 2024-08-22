// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The result of the pull operation.
/// </summary>
public class PullResult
{
    private readonly ConcurrentDictionary<Uri, ServiceResponse> _failedRequests = new();

    /// <summary>
    /// Determines if the pull result was completely successful.
    /// </summary>
    public bool IsSuccessful { get => this._failedRequests.IsEmpty; } 

    /// <summary>
    /// The total count of operations performed on this pull operation.
    /// </summary>
    public int OperationCount { get; } // TODO: Fill in the details.

    /// <summary>
    /// The list of failed requests.
    /// </summary>
    public IReadOnlyDictionary<Uri, ServiceResponse> FailedRequests { get => this._failedRequests.ToImmutableDictionary(); }

    /// <summary>
    /// Adds a failed request to the list of failed requests.
    /// </summary>
    /// <param name="requestUri">The request URI</param>
    /// <param name="response">The response object</param>
    internal void AddFailedRequest(Uri requestUri, ServiceResponse response)
        => _ = this._failedRequests.TryAdd(requestUri, response);
}
