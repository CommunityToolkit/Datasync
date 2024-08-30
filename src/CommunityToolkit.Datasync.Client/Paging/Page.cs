// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A response to a GetPageAsync request.
/// </summary>
/// <typeparam name="TEntity">The entity stored in the Items list.</typeparam>
public class Page<TEntity>
{
    /// <summary>
    /// The items in a page.
    /// </summary>
    public IEnumerable<TEntity> Items { get; set; } = [];

    /// <summary>
    /// The number of items that would be returned by the query,
    /// if not for paging.
    /// </summary>
    public long? Count { get; set; }

    /// <summary>
    /// The Uri to the next page in the result set.
    /// </summary>
    public string? NextLink { get; set; }
}
