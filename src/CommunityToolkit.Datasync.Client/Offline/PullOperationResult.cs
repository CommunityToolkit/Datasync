// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Internal;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The result of a single pull operation.
/// </summary>
public class PullOperationResult
{
    private readonly ConcurrentDictionary<Type, InternalOperationResult> _successfulRequests = new();
    private readonly ConcurrentDictionary<string, ServiceResponse> _failedRequests = new();

    /// <summary>
    /// True if the pull operation was successful.
    /// </summary>
    public bool IsSuccessful { get => this._failedRequests.IsEmpty; }

    /// <summary>
    /// The total number of operations against the database that were resolved.
    /// </summary>
    public int TotalOperations
    {
        get
        {
            int operations = 0;
            foreach (KeyValuePair<Type, InternalOperationResult> kv in this._successfulRequests)
            {
                operations += kv.Value.Additions + kv.Value.Deletions + kv.Value.Replacements;
            }

            return operations;
        }
    }

    /// <summary>
    /// Returns the mapping of requests and responses that failed.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceResponse> FailedRequests
    {
        get => this._failedRequests.ToImmutableDictionary();
    }

    /// <summary>
    /// Record an addition of an entity.
    /// </summary>
    /// <param name="entityType">The type of the entity added.</param>
    internal void AddAddition(Type entityType)
    {
        InternalOperationResult opResult = this._successfulRequests.GetOrAdd(entityType, _ => new InternalOperationResult());
        opResult.IncrementAdd();
    }

    /// <summary>
    /// Record a deletion of an entity.
    /// </summary>
    /// <param name="entityType">The type of the entity deleted.</param>
    internal void AddDeletion(Type entityType)
    {
        InternalOperationResult opResult = this._successfulRequests.GetOrAdd(entityType, _ => new InternalOperationResult());
        opResult.IncrementDelete();
    }

    /// <summary>
    /// Record a replacement of an entity.
    /// </summary>
    /// <param name="entityType">The type of the entity replaced.</param>
    internal void AddReplacement(Type entityType)
    {
        InternalOperationResult opResult = this._successfulRequests.GetOrAdd(entityType, _ => new InternalOperationResult());
        opResult.IncrementReplace();
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    /// <param name="operation">The operation being executed.</param>
    /// <param name="response">The response from the server.</param>
    internal void AddFailedRequest(ServicePullOperation operation, ServiceResponse response)
    {
        Uri baseUri = ExecutableOperation.MakeAbsoluteUri(operation.Client.BaseAddress, operation.Endpoint);
        string requestUri = new UriBuilder(baseUri) { Query = operation.QueryString }.Uri.ToString();
        _ = this._failedRequests.TryAdd(requestUri, response);
    }

    /// <summary>
    /// An internal tracking class for a single entity type.
    /// </summary>
    internal class InternalOperationResult
    {
        private int _additions = 0, _deletions = 0, _replacements = 0;

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
        /// Increments the additions.
        /// </summary>
        internal void IncrementAdd()
            => Interlocked.Increment(ref this._additions);

        /// <summary>
        /// Increments the deletions.
        /// </summary>
        internal void IncrementDelete()
            => Interlocked.Increment(ref this._deletions);

        /// <summary>
        /// Increments the replacements.
        /// </summary>
        internal void IncrementReplace()
            => Interlocked.Increment(ref this._replacements);
    }
}
