// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// Provides access to the platform-specific API.
/// </summary>
internal static class Platform
{
    /// <summary>
    /// Obtains the assembly version of the current assembly.
    /// </summary>
    /// <remarks>
    /// Version should never be null, so we can't really test the nullable 
    /// side of the coalesce operaiton.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal static string AssemblyVersion
        => typeof(Platform).Assembly.GetName().Version?.ToString() ?? string.Empty;

    /// <summary>
    /// The details section of the User-Agent header.
    /// </summary>
    internal static string UserAgentDetails
    {
        get => $"lang=Managed;os={RuntimeInformation.OSDescription}/{Environment.OSVersion.VersionString};arch={RuntimeInformation.OSArchitecture};version={Platform.AssemblyVersion}";
    }
}
