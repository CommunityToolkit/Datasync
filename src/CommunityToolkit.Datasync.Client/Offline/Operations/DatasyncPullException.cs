// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline.Operations;

/// <summary>
/// An internal exception used for reporting errors when pulling data from
/// the remote service.
/// </summary>
internal class DatasyncPullException : Exception
{
    public DatasyncPullException()
    {
    }

    public DatasyncPullException(string? message) : base(message)
    {
    }

    public DatasyncPullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The response from the service.
    /// </summary>
    public required ServiceResponse ServiceResponse { get; init; }
}
