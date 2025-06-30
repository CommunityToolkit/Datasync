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
    /// Pull for the given entity starts.
    /// </summary>
    /// <remarks><see cref="SynchronizationEventArgs.ItemsProcessed"/> is not yet known here</remarks>
    PullStarted,
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

    /// <summary>
    /// Pull for the given entiry ended.
    /// </summary>
    PullEnded,
    /// <summary>
    /// Push operation started.
    /// </summary>
    PushStarted,
    /// <summary>
    /// An item was pushed to the server
    /// </summary>
    PushItem,
    /// <summary>
    /// Push operation ended.
    /// </summary>
    PushEnded,
}

/// <summary>
/// The event arguments sent when a synchronization event occurs.
/// </summary>
public class SynchronizationEventArgs
{
    /// <summary>
    /// The type of event.
    /// </summary>
    /// <remarks>
    /// On pull events, reporting occurs per entity type. With a start/stop per entity type.
    /// On push events, reporting occurs per push request, which may contain multiple entity types.
    /// </remarks>
    public required SynchronizationEventType EventType { get; init; }

    /// <summary>
    /// The EntityType that is being processed. Not used on push events.
    /// </summary>
    public Type? EntityType { get; init; }

    /// <summary>
    /// When pulling records, the number of items for the given entiry that have been processed in the current pull request.
    /// When pushing records, the total number of items that have been processed in the current push request.
    /// </summary>
    public long ItemsProcessed { get; init; } = -1;

    /// <summary>
    /// When pulling records, the total number of items to pull for the given entity in the current pull request
    /// When pushing records, the total number of items that are being pushed in the current push request.
    /// </summary>
    public long ItemsTotal { get; init; }

    /// <summary>
    /// The query ID that is being processed on pull operations. Not used on push events.
    /// </summary>
    public string? QueryId { get; init; }

    /// <summary>
    /// If not <c>null</c> on event type <see cref="SynchronizationEventType.PullEnded"/>, indicates pull failed with this exception. Currently not used on push.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// If a <see cref="DatasyncException"/> occured in <see cref="Exception"/> during server call processing, this property has more detail on the server response. Currently not used on push, use the returned <see cref="PushResult.FailedRequests"/> instead.
    /// </summary>
    public ServiceResponse? ServiceResponse { get; init; }

    /// <summary>
    /// The operation that was executed. Not used on pull events.
    /// </summary>
    public DatasyncOperation? PushOperation { get; init; }
}
