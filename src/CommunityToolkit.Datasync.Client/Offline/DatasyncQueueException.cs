// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// An error occurred when updating the Datasync Operations Queue.
/// </summary>
public class DatasyncQueueException : DatasyncException
{
    /// <summary>
    /// Creates a new <see cref="DatasyncQueueException"/> based on the original and updated operations.
    /// </summary>
    /// <param name="originalOperation"></param>
    /// <param name="updatedOperation"></param>
    public DatasyncQueueException(DatasyncOperation originalOperation, DatasyncOperation updatedOperation)
        : base("The operation could not be updated due to an invalid state change.")
    {
        OriginalOperation = originalOperation;
        UpdatedOperation = updatedOperation;
    }

    /// <summary>
    /// The original operation definition.
    /// </summary>
    public DatasyncOperation OriginalOperation { get; }

    /// <summary>
    /// The updated operation definition.
    /// </summary>
    public DatasyncOperation UpdatedOperation { get; }
}
