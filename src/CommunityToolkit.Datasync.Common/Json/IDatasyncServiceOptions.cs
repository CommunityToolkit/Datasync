// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Common;

/// <summary>
/// The definition of the service options available when configuring a Datasync controller.
/// </summary>
/// <remarks>
/// This is used in dependency injection to configure the various attributes with standardized options.
/// </remarks>
public interface IDatasyncServiceOptions
{
    /// <summary>
    /// The serializer options to use for communication with a Datasync client.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; }
}
