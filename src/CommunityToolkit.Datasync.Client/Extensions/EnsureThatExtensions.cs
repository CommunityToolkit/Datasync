// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of extension methods for the EnsureThat Guard class.
/// </summary>
internal static partial class EnsureThatExtensions
{
    private static readonly Regex httpHeaderName = HttpHeaderNameRegex();

    /// <summary>
    /// Guards against invalid datasync service endpoint URIs.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <exception cref="UriFormatException">Thrown if the URI is not valid.</exception>
    internal static void IsValidDatasyncEndpoint(this Param<Uri> param)
    {
        param.IsNotNull();
        if (!param.Value.IsAbsoluteUri)
        {
            throw new UriFormatException($"The '{param.Name}' must be an absolute URI");
        }

        if (param.Value.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            if (!param.Value.IsLoopback)
            {
                throw new UriFormatException($"'{param.Name}' must use secure (https) endpoint when not loopback");
            }
        }
        else if (!param.Value.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            throw new UriFormatException($"'{param.Name}' must use HTTP endpoint");
        }
    }

    /// <summary>
    /// Guards against an invalid HTTP header name.
    /// </summary>
    /// <param name="param">The parameter to check.</param>
    /// <exception cref="ArgumentException">Thrown if the string is not a valid HTTP header name.</exception>
    internal static void IsValidHttpHeaderName(this StringParam param)
    {
        param.IsNotEmptyOrWhiteSpace();
        if (!httpHeaderName.IsMatch(param.Value))
        {
            throw new ArgumentException($"The '{param.Name}' must be a valid HTTP header name");
        }
    }

    /// <summary>
    /// Generates the regular expression at compile time for a HTTP header name.
    /// </summary>
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9-_]*$")]
    private static partial Regex HttpHeaderNameRegex();
}
