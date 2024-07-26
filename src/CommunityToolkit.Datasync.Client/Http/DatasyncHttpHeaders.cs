// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// The names of various headers that we use in communicating with the datasync service.
/// </summary>
internal class DatasyncHttpHeaders
{
    /// <summary>
    /// The conditional "If-Match" header.
    /// </summary>
    internal const string IfMatch = "If-Match";


    /// <summary>
    /// The <c>User-Agent</c> header name (so we don't need to bring in a NuGet for it)
    /// </summary>
    internal const string UserAgent = "User-Agent";

    /// <summary>
    /// The unique ID of the installation.
    /// </summary>
    [Obsolete("This is provided for backwards compatibility with Azure Mobile Apps and will be removed in a future version of the library.")]
    internal const string InstallationId = "X-ZUMO-INSTALLATION-ID";

    /// <summary>
    /// The version of the protocol being used.
    /// </summary>
    [Obsolete("This is provided for backwards compatibility with Azure Mobile Apps and will be removed in a future version of the library.")]
    internal const string ProtocolVersion = "ZUMO-API-VERSION";
}
