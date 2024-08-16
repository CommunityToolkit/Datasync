// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An <see cref="ArgumentException"/> that throws on a data validation exception.
/// </summary>
public class ArgumentValidationException : ArgumentException
{
    /// <inheritdocs />
    [ExcludeFromCodeCoverage]
    public ArgumentValidationException() : base() { }

    /// <inheritdocs />
    [ExcludeFromCodeCoverage]
    public ArgumentValidationException(string? message) : base(message) { }

    /// <inheritdocs />
    [ExcludeFromCodeCoverage]
    public ArgumentValidationException(string? message, Exception? innerException) : base(message, innerException) { }

    /// <inheritdocs />
    [ExcludeFromCodeCoverage]
    public ArgumentValidationException(string? message, string? paramName) : base(message, paramName) { }

    /// <inheritdocs />
    [ExcludeFromCodeCoverage]
    public ArgumentValidationException(string? message, string? paramName, Exception? innerException) : base(message, paramName, innerException) { }

    /// <summary>
    /// The list of validation errors.
    /// </summary>
    public IList<ValidationResult>? ValidationErrors { get; private set; }

    /// <summary>
    /// Throws an exception if the object is not valid according to data annotations.
    /// </summary>
    /// <param name="value">The value being tested.</param>
    /// <param name="paramName">The name of the parameter.</param>
    public static void ThrowIfNotValid(object? value, string? paramName)
      => ThrowIfNotValid(value, paramName, "Object is not valid");

    /// <summary>
    /// Throws an exception if the object is not valid according to data annotations.
    /// </summary>
    /// <param name="value">The value being tested.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="message">The message for the object.</param>
    public static void ThrowIfNotValid(object? value, string? paramName, string? message)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        List<ValidationResult> results = [];
        if (!Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true))
        {
            throw new ArgumentValidationException(message, paramName) { ValidationErrors = results };
        }
    }
}
