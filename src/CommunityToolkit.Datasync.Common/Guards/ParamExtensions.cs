// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Guards;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Datasync.Common;

/// <summary>
/// A set of extension methods to use with the guard statements.
/// </summary>
public static partial class ParamExtensions
{
    /// <summary>
    /// Checks to see if a list has a specific number of elements.
    /// </summary>
    /// <typeparam name="T">The type contained within the list.</typeparam>
    /// <param name="param">The parameter to check.</param>
    /// <param name="expectedCount">The expected count.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Throws if the list has an incorrect number of items.</exception>
    public static Param<IList<T>> HasCount<T>(this Param<IList<T>> param, int expectedCount, string? because = null)
    {
        if (param.Value.Count != expectedCount)
        {
            because ??= $"The parameter '{param.Name}' must have exactly {expectedCount} items";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Checks to see if a list has at least 1 elements.
    /// </summary>
    /// <typeparam name="T">The type contained within the list.</typeparam>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Throws if the list has an incorrect number of items.</exception>
    public static Param<IList<T>> HasItems<T>(this Param<IList<T>> param, string? because = null)
    {
        if (param.Value.Count == 0)
        {
            because ??= $"The parameter '{param.Name}' must have some items";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Guards against a URI being a relative URI.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="UriFormatException">Thrown if the URI provided is not an absolute URI.</exception>
    public static Param<Uri> IsAbsoluteUri(this Param<Uri> param, string? because = null)
    {
        if (!param.Value.IsAbsoluteUri)
        {
            because ??= $"The parameter '{param.Name}' must be an absolute URI";
            throw new UriFormatException(because);
        }

        return param;
    }

    /// <summary>
    /// Guards against invalid datasync service endpoint URIs.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="UriFormatException">Thrown if the URI is not valid.</exception>
    public static Param<Uri> IsDatasyncEndpoint(this Param<Uri> param, string? because = null)
        => param.IsNotNull().And.IsAbsoluteUri(because).And.IsLoopbackOrHttps(because);

    /// <summary>
    /// Checks that the provided integer is greater than the provided value.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="value">The minimum expected value.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Thrown if the parameter value is less than or equal to the provided value.</exception>
    public static Param<int> IsGt(this Param<int> param, int value, string? because = null)
    {
        if (param.Value <= value)
        {
            because ??= $"The parameter '{param.Name}' must be greater than {value}";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Checks that the provided integer is greater than or equal to the provided value.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="value">The minimum expected value.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Thrown if the parameter value is less than the provided value.</exception>
    public static Param<int> IsGte(this Param<int> param, int value, string? because = null)
    {
        if (param.Value < value)
        {
            because ??= $"The parameter '{param.Name}' must be greater than or equal to {value}";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Checks that the provided TimeSpan is greater than or equal to the provided value.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="value">The minimum expected value.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Thrown if the parameter value is less than the provided value.</exception>
    public static Param<TimeSpan> IsGte(this Param<TimeSpan> param, TimeSpan value, string? because = null)
    {
        if (param.Value < value)
        {
            because ??= $"The parameter '{param.Name}' must be greater than or equal to {value}";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Checks to ensure that the provided string parameter matches the regular expression for a HTTP header name.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason used in generating exceptions.</param>
    /// <returns>The parameter (for chaining).</returns>
    public static Param<string> IsHttpHeaderName(this Param<string> param, string? because = null)
        => param.IsNotNull().And.Matches(RegexpConstants.HttpHeaderName, because ?? "The parameter must be a valid HTTP header name");

    /// <summary>
    /// Checks that an integer parameter is within a specified range.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="minValue">The minimum value for the parameter.</param>
    /// <param name="maxValue">The maximum value for the parameter.</param>
    /// <param name="because">A reason used in generating exceptions.</param>
    /// <returns>The parameter (for chaining).</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Param<int> IsInRange(this Param<int> param, int minValue, int maxValue, string? because = null)
    {
        if (param.Value < minValue || param.Value > maxValue)
        {
            because ??= $"The parameter '{param.Name}' must be between {minValue} and {maxValue}";
            throw new ArgumentOutOfRangeException(param.Name, param.Value, because);
        }

        return param;
    }

    /// <summary>
    /// Guards against a URI not being a valid HTTP or HTTPS endpoint.  Loopback URIs are allowed
    /// to be HTTP, but all remote URIs must be HTTPS.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="UriFormatException">Thrown if the URI is not a valid HTTP or HTTPS endpoint.</exception>
    public static Param<Uri> IsLoopbackOrHttps(this Param<Uri> param, string? because = null)
    {
        if (param.Value.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            if (!param.Value.IsLoopback)
            {
                because ??= $"'{param.Name}' must use secure (https) endpoint when not loopback";
                throw new UriFormatException(because);
            }
        }
        else if (!param.Value.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            because ??= $"'{param.Name}' must use HTTP or HTTPS scheme";
            throw new UriFormatException(because);
        }

        return param;
    }

    /// <summary>
    /// Checks to ensure the provided string parameter is not null or empty.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Thrown if the parameter is null or empty.</exception>
    public static Param<string> IsNotNullOrEmpty(this Param<string> param, string? because = null)
    {
        _ = param.IsNotNull();
        if (string.IsNullOrEmpty(param.Value))
        {
            because ??= $"The parameter '{param.Name}' must not be null or empty";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Checks to ensure the provided string parameter is not null or white space.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Thrown if the parameter is null or white space.</exception>
    public static Param<string> IsNotNullOrWhiteSpace(this Param<string> param, string? because = null)
    {
        _ = param.IsNotNull();
        if (string.IsNullOrWhiteSpace(param.Value))
        {
            because ??= $"The parameter '{param.Name}' must not be null or whitespace";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Guards against a string not being a valid Id.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="because">A reason to use instead of the default reason.</param>
    /// <returns>The parameter (for chaining)</returns>
    /// <exception cref="ArgumentException">Thrown if the ID is not valid.</exception>
    public static Param<string> IsValidId(this Param<string> param, string? because = null)
    {
        if (!RegexpConstants.EntityIdentity.IsMatch(param.Value))
        {
            because ??= $"The parameter '{param.Name}' must be a valid Id";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }

    /// <summary>
    /// Checks to ensure that the provided string parameter matches a provided regular expression.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <param name="regularExpression">The required regular expression.</param>
    /// <param name="because">A readon</param>
    /// <returns>The parameter (for chaining).</returns>
    /// <exception cref="ArgumentException">Thrown if the parameter does not match the regular expression.</exception>
    public static Param<string> Matches(this Param<string> param, Regex regularExpression, string? because = null)
    {
        if (!regularExpression.IsMatch(param.Value))
        {
            because ??= $"The parameter must match the regular expression '{regularExpression}'";
            throw new ArgumentException(because, param.Name);
        }

        return param;
    }
}
