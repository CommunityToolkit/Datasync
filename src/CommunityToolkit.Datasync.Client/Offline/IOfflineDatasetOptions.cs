// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The set of options that can be used to configure the offline dataset.
/// </summary>
public interface IOfflineDatasetOptions
{
    /// <summary>
    /// An optional function that can be used to generate a new ID for an entity.
    /// </summary>
    Func<string> GenerateId { get; }
}
