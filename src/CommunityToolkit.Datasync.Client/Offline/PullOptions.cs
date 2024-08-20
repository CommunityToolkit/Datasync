// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The options to use for pulling operations to the service.
/// </summary>
public class PullOptions
{
    /// <summary>
    /// The number of parallel operations to use for the push operation.
    /// </summary>
    [Required, Range(1, 8, ErrorMessage = "Invalid number of parallel operations.")]
    public int ParallelOperations { get; set; } = 1;

    /// <summary>
    /// If set, all changes from a push operation are automatically saved at
    /// the end of the push operations.
    /// </summary>
    /// <remarks>
    /// Bad things happen if you don't autosave - only do this if you are testing
    /// specific locking and threading issues.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal bool AutoSave { get; set; } = true;
}
