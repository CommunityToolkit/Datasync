// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Models;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The results of a pull operation.
/// </summary>
public class PullResult
{
    /// <summary>
    /// If <c>true</c>, the complete operation was successful.
    /// </summary>
    public bool IsSuccessful { get; internal set; } = false;

    /// <summary>
    /// The individual operation results.
    /// </summary>
    public IEnumerable<PullOperationResult> Operations { get; internal set; } = [];
}
