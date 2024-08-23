// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Exceptions;

internal class DatasyncPullException : DatasyncException
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
    /// The service response for the error.
    /// </summary>
    public required ServiceResponse ServiceResponse { get; init; }
}
