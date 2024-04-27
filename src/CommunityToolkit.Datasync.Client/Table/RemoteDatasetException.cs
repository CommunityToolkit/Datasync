// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The base exception for all exceptions that caused when the remote service returns an error.
/// </summary>
[ExcludeFromCodeCoverage]
public class RemoteDatasetException : ApplicationException
{
    /// <inheritdoc />
    public RemoteDatasetException() : base()
    {
    }

    /// <inheritdoc />
    public RemoteDatasetException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public RemoteDatasetException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The HTTP status code that was returned.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }
}
