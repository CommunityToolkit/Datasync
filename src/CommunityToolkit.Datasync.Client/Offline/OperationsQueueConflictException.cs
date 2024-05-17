// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception that is thrown when adding or updating an operation in the operations
/// queue that causes a conflict in the operations.  Examples of this include adding or
/// updating an entity after it has been deleted
/// </summary>
public class OperationsQueueConflictException : OfflineDatasetException
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor")]
    public OperationsQueueConflictException() : base()
    {
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor")]
    public OperationsQueueConflictException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor")]
    public OperationsQueueConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The queue entity that caused the conflict.
    /// </summary>
    public OfflineQueueEntity QueuedOperation { get; init; }

    /// <summary>
    /// The entity being introduced to the queue that caused the conflict.
    /// </summary>
    public object ConflictingEntity { get; init; }
}
