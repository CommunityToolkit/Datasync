// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The options to use for pushing operations to the service.
/// </summary>
public class PullOptions
{
    /// <summary>
    /// The number of parallel operations to use for the push operation.
    /// </summary>
    [Required, Range(1, 8, ErrorMessage = "Invalid number of parallel operations.")]
    public int ParallelOperations { get; set; } = 1;

    /// <summary>
    /// If true, SaveChangesAsync() is called after every service request is received.
    /// </summary>
    public bool SaveAfterEveryServiceRequest { get; set; } = true;
}
