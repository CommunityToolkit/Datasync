// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Service;

/// <summary>
/// The options that can be used to modify the request for the service.
/// </summary>
/// <remarks>
/// Not all options are available for all operations.
/// </remarks>
public class DatasyncServiceOptions
{
    /// <summary>
    /// If set, then the version will be sent as an If-Match or If-None-Match header as appropriate.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// If true, then an EntityDoesNotExistException is thrown if the entity requested does not exist.
    /// If false, then the normal <see cref="ServiceResponse"/> is returned but with IsSuccessful set
    /// to false.
    /// </summary>
    public bool ThrowIfMissing { get; set; }
}
