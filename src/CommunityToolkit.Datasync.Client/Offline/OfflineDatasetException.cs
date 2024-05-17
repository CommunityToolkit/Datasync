// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A base exception class for all the exceptions thrown by the offline dataset processing.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Standard exception class")]
public class OfflineDatasetException : ApplicationException
{
    /// <inheritdoc />
    public OfflineDatasetException() : base()
    {
    }

    /// <inheritdoc />
    public OfflineDatasetException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public OfflineDatasetException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
