// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The list of synchronization events that we support.
/// </summary>
public enum SynchronizationEventType
{
    /// <summary>
    /// Occurs when items have been successfully fetches from the server.
    /// </summary>
    /// <remarks>This event is raised after a page of entities was succesfully fetched from the server, ready to be commited to the data store.</remarks>
    ItemsFetched,

    /// <summary>
    /// Occurs when items have been successfully committed to the underlying data store.
    /// </summary>
    /// <remarks>This event is raised after a page of entities was succesfully commited to the database</remarks>
    ItemsCommitted,
}

/// <summary>
/// The event arguments sent when a synchronization event occurs.
/// </summary>
public class SynchronizationEventArgs
{
    /// <summary>
    /// The type of event.
    /// </summary>
    public required SynchronizationEventType EventType { get; init; }

    /// <summary>
    /// The EntityType that is being processed.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// When pulling records, the number of items that have been processed in the current pull request.
    /// </summary>
    public long ItemsProcessed { get; init; } = -1;

    /// <summary>
    /// The total number of items in the current pull request.
    /// </summary>
    public long TotalNrItems { get; init; }

    /// <summary>
    /// The query ID that is being processed
    /// </summary>
    public required string QueryId { get; init; }
}
