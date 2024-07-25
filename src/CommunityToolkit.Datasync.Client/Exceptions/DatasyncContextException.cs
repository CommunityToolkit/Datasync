// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Context;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception that is thrown when there is a problem with the <see cref="DatasyncContext"/>
/// </summary>
public class DatasyncContextException : DatasyncException
{
    /// <inheritdoc />
    public DatasyncContextException()
    {
    }

    /// <inheritdoc />
    public DatasyncContextException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public DatasyncContextException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
