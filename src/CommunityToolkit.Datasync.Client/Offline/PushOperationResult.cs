// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The result of a single push operation.
/// </summary>
public class PushOperationResult
{
    internal int _completedOperations = 0;
    internal readonly ConcurrentDictionary<string, ServiceResponse> _failedOperations = new();

    /// <summary>
    /// The number of completed operations.
    /// </summary>
    public int CompletedOperations { get => this._completedOperations; }

    /// <summary>
    /// The number of failed operations.
    /// </summary>
    public IDictionary<string, ServiceResponse> FailedOperations { get => this._failedOperations.ToImmutableDictionary(); }

    /// <summary>
    /// Determines if the operation was successful.
    /// </summary>
    public bool IsSuccessful { get => this._failedOperations.IsEmpty; }

    /// <summary>
    /// Adds an operation result in a thread safe manner.
    /// </summary>
    /// <param name="operationId">The ID of the operation being processed.</param>
    /// <param name="response">The response from the service.</param>
    internal void AddOperationResult(string operationId, ServiceResponse? response)
    {
        if (response is null)
        {
            _ = Interlocked.Increment(ref this._completedOperations);
        }
        else
        {
            _ = this._failedOperations.TryAdd(operationId, response);
        }
    }
}