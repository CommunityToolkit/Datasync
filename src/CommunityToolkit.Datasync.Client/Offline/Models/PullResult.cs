// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
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
    private readonly ConcurrentDictionary<string, Exception> _localExceptions = new();

    /// <summary>
    /// Determines if the pull result was completely successful.
    /// </summary>
    public bool IsSuccessful { get => this._failedRequests.IsEmpty && this._localExceptions.IsEmpty; } 

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
    /// The list of failed requests.  The key is the request URI, and the value is 
    /// the <see cref="ServiceResponse"/> for that request.
    /// </summary>
    public IReadOnlyDictionary<Uri, ServiceResponse> FailedRequests { get => this._failedRequests.ToImmutableDictionary(); }

    /// <summary>
    /// The list of local exceptions.  The key is the GUID of the entity that caused the exception, 
    /// and the value is the exception itself.
    /// </summary>
    public IReadOnlyDictionary<string, Exception> LocalExceptions { get => this._localExceptions.ToImmutableDictionary(); }

    /// <summary>
    /// Adds a failed request to the list of failed requests.
    /// </summary>
    /// <param name="requestUri">The request URI causing the failure.</param>
    /// <param name="response">The response for the request.</param>
    internal void AddFailedRequest(Uri requestUri, ServiceResponse response)
        => _ = this._failedRequests.TryAdd(requestUri, response);

    /// <summary>
    /// Adds a local exception to the list of local exceptions.
    /// </summary>
    /// <param name="entityMetadata">The entity metadata, or null if not available.</param>
    /// <param name="exception">The exception that was thrown.</param>
    internal void AddLocalException(EntityMetadata? entityMetadata, Exception exception)
    {
        string entityId = entityMetadata?.Id ?? $"NULL:{Guid.NewGuid():N}";
        _ = this._localExceptions.TryAdd(entityId, exception);
    }

    internal void IncrementAdditions()
        => Interlocked.Increment(ref this._additions);

    internal void IncrementDeletions()
        => Interlocked.Increment(ref this._deletions);

    internal void IncrementReplacements()
        => Interlocked.Increment(ref this._replacements);
}
