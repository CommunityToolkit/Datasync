// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// Definition of the operations queue manager.  This is the set of methods used to
/// maintain the queue of operations that need to be synchronized with the remote service.
/// </summary>
internal interface IOperationsQueueManager
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing data.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// Creates a new Create/Insert/Add operation for the given state change.
    /// </summary>
    /// <param name="entry">The entry being processed.</param>
    void AddCreateOperation(EntityEntry entry);

    /// <summary>
    /// Creates a new Delete operation for the given state change.
    /// </summary>
    /// <param name="entry">The entry being processed.</param>
    void AddDeleteOperation(EntityEntry entry);

    /// <summary>
    /// Creates a new Modify/Update operation for the given state change.
    /// </summary>
    /// <param name="entry">The entry being processed.</param>
    void AddUpdateOperation(EntityEntry entry);
}
