// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// The base exception class for all exceptions thrown by the Datasync Toolkit.
/// </summary>
[ExcludeFromCodeCoverage]
public class DatasyncToolkitException : Exception
{
    /// <inheritdoc />
    public DatasyncToolkitException() : base()
    {
    }

    /// <inheritdoc />
    public DatasyncToolkitException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public DatasyncToolkitException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
