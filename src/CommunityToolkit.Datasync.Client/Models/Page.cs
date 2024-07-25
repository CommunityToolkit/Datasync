// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The model for the response from a query operation.
/// </summary>
public class Page<T>
{
    /// <summary>
    /// The items in a page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// The number of items that would be returned by the query,
    /// if not for paging.
    /// </summary>
    public long? Count { get; set; }

    /// <summary>
    /// The Uri to the nexty page in the result set.
    /// </summary>
    public string? NextLink { get; set; }
}
