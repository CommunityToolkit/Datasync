// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of methods that throw errors if the conditions are not met.
/// </summary>
internal static class ThrowIf
{
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
}
