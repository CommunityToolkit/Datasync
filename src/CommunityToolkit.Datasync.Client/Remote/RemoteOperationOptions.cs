// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of options to modify the capabilities of the remote dataset operations.
/// </summary>
public struct RemoteOperationOptions
{
    /// <summary>
    /// If <c>true</c>, the operation will consider deleted items to be in scope.
    /// </summary>
    public bool IncludeDeletedItems { get; set; }

    /// <summary>
    /// If set, the server entity must match this version for the operation to succeed.
    /// </summary>
    public string? RequiredVersion { get; set; }
}
