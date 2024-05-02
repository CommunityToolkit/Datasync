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
    public RemoteDatasetException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public RemoteDatasetException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The HTTP status code that was returned.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }
}

/// <summary>
/// An exception that is thrown when the server reports a conflict in the operation.
/// </summary>
/// <typeparam name="T">The type of the entity being processed.</typeparam>
[ExcludeFromCodeCoverage]
[SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "Specialized exception.")]
public class ConflictException<T> : RemoteDatasetException
{
    /// <summary>
    /// Creates a new <see cref="ConflictException{T}"/> instance.
    /// </summary>
    /// <param name="message">The message that was provided by the server.</param>
    /// <param name="entity">The server version of the entity causing the conflict.</param>
    public ConflictException(string message, T entity) : base(message)
    {
        Entity = entity;
        StatusCode = HttpStatusCode.Conflict;
    }

    /// <summary>
    /// The server entity that caused the conflict.
    /// </summary>
    public T Entity { get; set; }
}
