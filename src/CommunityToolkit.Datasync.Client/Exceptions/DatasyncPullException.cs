// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client.Exceptions;

/// <summary>
/// An internal exception generated during the pull operation.
/// </summary>
internal class DatasyncPullException : DatasyncException
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public DatasyncPullException()
    {
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public DatasyncPullException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public DatasyncPullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The service response for the error.
    /// </summary>
    public required ServiceResponse ServiceResponse { get; init; }
}
