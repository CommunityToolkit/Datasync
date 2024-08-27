// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The result of a single push operation.
/// </summary>
public class PushResult
{
    internal int _completedOperations = 0;
    internal readonly ConcurrentDictionary<string, ServiceResponse> _failedRequests = new();

    /// <summary>
    /// The number of completed operations.
    /// </summary>
    public int CompletedOperations { get => this._completedOperations; }

    /// <summary>
    /// The number of failed operations.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceResponse> FailedRequests { get => this._failedRequests.ToImmutableDictionary(); }

    /// <summary>
    /// Determines if the operation was successful.
    /// </summary>
    public bool IsSuccessful { get => this._failedRequests.IsEmpty; }

    /// <summary>
    /// Adds an operation result in a thread safe manner.
    /// </summary>
    /// <param name="operation">The operation being processed.</param>
    /// <param name="response">The response from the service.</param>
    internal void AddOperationResult(DatasyncOperation operation, ServiceResponse? response)
    {
        if (response is null)
        {
            _ = Interlocked.Increment(ref this._completedOperations);
        }
        else
        {
            _ = this._failedRequests.TryAdd(operation.Id, response);
        }
    }
}
