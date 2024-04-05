// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// An exception class that represents an extraordinary HTTP response.
/// </summary>
[SuppressMessage("Roslynator", "RCS1194:Implement exception constructors.", Justification = "This is a specialist exception that requires the status code to be present.")]
[ExcludeFromCodeCoverage]
public class HttpException : DatasyncToolkitException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to use when responding to the cleint.</param>
    public HttpException(int statusCode) : base()
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpException"/> class with a specified error message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to use.</param>
    /// <param name="message">The message that describes the error.</param>
    public HttpException(int statusCode, string? message) : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to use.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that was the cause of the error.</param>
    public HttpException(int statusCode, string? message, Exception? innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// The HTTP status code to use when responding to the client.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// If set, the payload for the body of the HTTP exception.
    /// </summary>
    public object? Payload { get; set; }
}
