// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of extension methods for the EnsureThat library.
/// </summary>
internal static class EnsureThatExtensions
{
    /// <summary>
    /// Returns if the parameter is a valid endpoint for a Datasync service.
    /// </summary>
    /// <remarks>
    /// A valid endpoint is a HTTP endpoint (when using a loopback address) or
    /// an absolute HTTP URI.
    /// </remarks>
    /// <param name="param">The parameter to check</param>
    /// <exception cref="ArgumentNullException">if the endpoint is null.</exception>
    /// <exception cref="UriFormatException">if the endpoint is not valid.</exception>
    internal static void IsValidDatasyncUri(this Param<Uri> param)
    {
        param.HasValue();

        Uri endpoint = param.Value;
        if (!endpoint.IsAbsoluteUri)
        {
            throw new UriFormatException($"The parameter '{param.Name}' must be an absolute Uri");
        }

        if (endpoint.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!endpoint.IsLoopback)
            {
                throw new UriFormatException($"'{param.Name}' must use secure (https) endpoint when not loopback");
            }
        }
        else if (!endpoint.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new UriFormatException($"'{param.Name}' must use HTTP protocol");
        }
    }

    /// <summary>
    /// Determines if the given string parameter is a valid remote parameter.
    /// </summary>
    /// <param name="param">The string parameter to check.</param>
    /// <exception cref="ArgumentNullException">Thrown if the parameter is empty.</exception>
    /// <exception cref="ArgumentException">Thrown if the parameter is invalid.</exception>
    internal static void IsValidRemoteParameter(this StringParam param)
    {
        if (string.IsNullOrWhiteSpace(param.Value)) 
        {
            throw new ArgumentNullException($"The parameter '{param.Name}' cannot be empty or whitespace.");
        }
        
        if (param.Value.StartsWith('$') || param.Value.StartsWith("__"))
        {
            throw new ArgumentException($"The parameter '{param.Name}' has an invalid format.");
        }
    }
}
