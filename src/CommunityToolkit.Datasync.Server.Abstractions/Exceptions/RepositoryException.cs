// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// An exception to indicate that an error occurred in the repository implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class RepositoryException : DatasyncToolkitException
{
    /// <inheritdoc />
    public RepositoryException() : base()
    {
    }

    /// <inheritdoc />
    public RepositoryException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public RepositoryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
