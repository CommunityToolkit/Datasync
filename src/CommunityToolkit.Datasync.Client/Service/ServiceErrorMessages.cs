// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Service;

/// <summary>
/// A set of error messages used by the service client.
/// </summary>
internal static class ServiceErrorMessages
{
    /// <summary>
    /// The HttpClient generator has already been set.
    /// </summary>
    internal static string HttpClientGeneratorAlreadySet = "The HttpClient generator has already been set.";

    /// <summary>
    /// The content received from the service was invalid.
    /// </summary>
    internal static string InvalidContent = "The content received from the service is invalid.";

    /// <summary>
    /// Provided version is invalid.
    /// </summary>
    internal static string InvalidVersion = "The version string cannot contain illegal characters.";

    /// <summary>
    /// When initializing the datasync service connector, no HttpClient generator was created.
    /// </summary>
    internal static string NoHttpClientGenerator = "No HttpClient generator was configured in the OnDatasyncInitializing() method of your OfflineDbContext.";

    /// <summary>
    /// Server expected to send content, but returned a successful response without content.
    /// </summary>
    internal static string NoContent = "Content was expected but not received from the service.";
}
