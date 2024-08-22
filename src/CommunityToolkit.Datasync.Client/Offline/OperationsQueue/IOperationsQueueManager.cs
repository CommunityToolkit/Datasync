// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline.OperationsQueue;

/// <summary>
/// The definition of the required parts of the operations queue manager used
/// by the pull operation.
/// </summary>
internal interface IOperationsQueueManager
{
    /// <summary>
    /// Counts the number of operations enqueued in the operations queue for the given
    /// entity type.
    /// </summary>
    /// <param name="entityType">The entity type being processed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The number of operations in queue for the entity.</returns>
    Task<int> CountOperationsInQueueAsync(Type entityType, CancellationToken cancellationToken = default);
}
