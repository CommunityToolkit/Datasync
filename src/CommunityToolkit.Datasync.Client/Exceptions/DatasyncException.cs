// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A baseline exception for all exceptions generated by the datasync system.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Standard exception constructors")]
public class DatasyncException : Exception
{
    /// <inheritdoc />
    public DatasyncException()
    {
    }

    /// <inheritdoc />
    public DatasyncException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public DatasyncException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
