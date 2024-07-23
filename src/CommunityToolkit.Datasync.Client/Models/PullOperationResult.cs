// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Models;

/// <summary>
/// The result of a single pull operation.
/// </summary>
public class PullOperationResult
{
    /// <summary>
    /// If <c>true</c>, the operation was successful.
    /// </summary>
    public bool IsSuccessful { get; internal set; }
}
