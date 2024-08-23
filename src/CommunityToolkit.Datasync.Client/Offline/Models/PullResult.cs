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
    private int _additions, _deletions, _replacements;
    private readonly ConcurrentDictionary<Uri, ServiceResponse> _failedRequests = new();

    /// <summary>
    /// Determines if the pull result was completely successful.
    /// </summary>
    public bool IsSuccessful { get => this._failedRequests.IsEmpty; } 

    /// <summary>
    /// The total count of operations performed on this pull operation.
    /// </summary>
    public int OperationCount { get => this._additions + this._deletions + this._replacements; }

    /// <summary>
    /// The number of additions.
    /// </summary>
    public int Additions { get => this._additions; }

    /// <summary>
    /// The number of deletions.
    /// </summary>
    public int Deletions { get => this._deletions; }

    /// <summary>
    /// The number of replacements.
    /// </summary>
    public int Replacements { get => this._replacements; }

    /// <summary>
    /// The list of failed requests.
    /// </summary>
    public IReadOnlyDictionary<Uri, ServiceResponse> FailedRequests { get => this._failedRequests.ToImmutableDictionary(); }

    internal void AddFailedRequest(Uri requestUri, ServiceResponse response)
        => _ = this._failedRequests.TryAdd(requestUri, response);

    internal void IncrementAdditions()
        => Interlocked.Increment(ref this._additions);

    internal void IncrementDeletions()
        => Interlocked.Increment(ref this._deletions);

    internal void IncrementReplacements()
        => Interlocked.Increment(ref this._replacements);
}
