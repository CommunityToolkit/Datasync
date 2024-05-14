// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The default implementation of the <see cref="IOfflineDatasetOptions"/> interface, for
/// configuring the offline dataset.
/// </summary>
public class OfflineDatasetOptions : IOfflineDatasetOptions
{
    /// <summary>
    /// Default implementation of the <see cref="IOfflineDatasetOptions.GenerateId"/> method,
    /// which uses a GUID to generate a unique ID..
    /// </summary>
    public Func<string> GenerateId { get; set; } = () => Guid.NewGuid().ToString("N");
}
