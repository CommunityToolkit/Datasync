// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Remote;

/// <summary>
/// The model class for the response to a paged request.
/// </summary>
public class Page<T>()
{
    /// <summary>
    /// The list of entities to include in the response.
    /// </summary>
    public IList<T> Items { get; set; } = [];

    /// <summary>
    /// The count of all the entities to be returned by the search (without paging).
    /// </summary>
    public long? Count { get; set; }

    /// <summary>
    /// The arguments to retrieve the next page of items.  The client needs to prepend
    /// the URI of the table to this.
    /// </summary>
    public string NextLink { get; set; }
}
