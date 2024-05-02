// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// This exception is thrown when the entity is invalid for use with the datasync service.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Standard exception type")]
public class InvalidEntityException : ApplicationException
{
    /// <inheritdoc />
    public InvalidEntityException() : base()
    {
    }

    /// <inheritdoc />
    public InvalidEntityException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public InvalidEntityException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new <see cref="InvalidEntityException"/> with a message and the name of the entity that is invalid.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="entityName">The name of the entity that is invalid.</param>
    public InvalidEntityException(string message, string entityName) : base(message)
    {
        EntityName = entityName;
    }

    /// <summary>
    /// The name of the entity that is invalid.
    /// </summary>
    public string EntityName { get; set; }
}
