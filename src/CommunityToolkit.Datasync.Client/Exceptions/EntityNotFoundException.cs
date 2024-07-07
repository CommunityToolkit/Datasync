// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception that indicates the requested entity does not exist.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Standard exception")]
public class EntityNotFoundException : DatasyncException
{
    /// <inheritdoc />
    public EntityNotFoundException()
    {
    }

    /// <inheritdoc />
    public EntityNotFoundException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public EntityNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
