// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

using CommunityToolkit.Datasync.Client.Serialization;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of methods that throw errors if the conditions are not met.
/// </summary>
internal static class ThrowIf
{
    /// <summary>
    /// An ETag is defined here: https://httpwg.org/specs/rfc9110.html#field.etag but becomes
    /// 0x21, 0x23-0x7E
    /// </summary>
    private static readonly Regex ValidETag = new("^[\x21\x23-\x7E]{0,126}$", RegexOptions.Compiled);

    /// <summary>
    /// Returns if the parameter does not have the required number of elements.
    /// </summary>
    /// <typeparam name="T">The type of the iterator in the parameter.</typeparam>
    /// <param name="values">The values being counted.</param>
    /// <param name="count">The expected count.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown if the count does not match.</exception>
    internal static void CountMismatch<T>(IEnumerable<T> values, int count, string paramName)
    {
        ArgumentNullException.ThrowIfNull(values, paramName);
        if (values.Count() != count)
        {
            throw new ArgumentException($"Parameter {paramName} should have {count} items.");
        }
    }

    /// <summary>
    /// Throws if the value is not a valid entity ID (allowing for null optionally)
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="because">The reason.</param>
    /// <param name="allowNull">If true, null is allowed as a value.</param>
    /// <exception cref="ArgumentException">Thrown if the value is not valid.</exception>
    internal static void EntityIdIsInvalid(string? value, string paramName, string because = "Argument is invalid", bool allowNull = false)
    {
        if (!EntityResolver.EntityIdIsValid(value, allowNull))
        {
            throw new ArgumentException(because, paramName);
        }
    }

    /// <summary>
    /// Throws if the value is not a valid ETag according to https://httpwg.org/specs/rfc9110.html#field.etag
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="because">The reason.</param>
    /// <exception cref="ArgumentException">Thrown if the value is not valid.</exception>
    internal static void IsInvalidETag(string value, string paramName, string? because = "The version is invalid")
    {
        if (!ValidETag.IsMatch(value))
        {
            throw new ArgumentException(because, paramName);
        }
    }

    /// <summary>
    /// Throws if the value provided is not null - in other words, the reverse of ArgumentNullException.ThrowIfNull()
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="because">The reason.</param>
    /// <exception cref="ArgumentException">Thrown if the value is not null.</exception>
    internal static void IsNotNull(object? value, string paramName, string because = "Argument cannot be null.")
    {
        if (value is not null)
        {
            throw new ArgumentException(because, paramName);
        }
    }

    /// <summary>
    /// Throws if the value provided is not null or empty - in other words, the reverse of ArgumentNullException.ThrowIfNullOrEmpty()
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="because">The reason.</param>
    /// <exception cref="ArgumentException">Thrown if the value is not null.</exception>
    internal static void IsNotNullOrEmpty(string? value, string paramName, string because = "Argument cannot be null.")
    {
        if (!string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(because, paramName);
        }
    }

    /// <summary>
    /// Returns if the parameter is a valid endpoint for a Datasync service.
    /// </summary>
    /// <remarks>
    /// A valid endpoint is a HTTP endpoint (when using a loopback address) or an absolute HTTP URI.
    /// </remarks>
    /// <param name="endpoint">The endpoint to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">if the endpoint is null.</exception>
    /// <exception cref="UriFormatException">if the endpoint is not valid.</exception>
    internal static void IsNotValidEndpoint(Uri endpoint, string paramName)
    {
        ArgumentNullException.ThrowIfNull(endpoint, paramName);
        if (!endpoint.IsAbsoluteUri)
        {
            throw new UriFormatException($"'{paramName}' must use an absolute URI");
        }

        if (endpoint.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!endpoint.IsLoopback)
            {
                throw new UriFormatException($"'{paramName}' must use secure (https) endpoint when not loopback");
            }
        }
        else if (!endpoint.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new UriFormatException($"'{paramName}' must use HTTP protocol");
        }
    }

    /// <summary>
    /// Throws if the provided enumeration is null or empty.
    /// </summary>
    /// <typeparam name="T">The generic type enumeration</typeparam>
    /// <param name="value">The value of the enumeration.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentNullException">if the enumeration is null.</exception>
    /// <exception cref="ArgumentException">if the enumeration is empty.</exception>
    internal static void NullOrEmpty<T>(IEnumerable<T> value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        if (!value.Any())
        {
            throw new ArgumentException("Argument is empty.", paramName);
        }
    }
}
