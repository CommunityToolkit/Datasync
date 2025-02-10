// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// The model class for the response to a paged request.
/// </summary>
/// <param name="items">The items being returned in the paged request.</param>
public class PagedResult(IEnumerable<object>? items = null)
{
    /// <summary>
    /// The list of entities to include in the response.
    /// </summary>
    public IEnumerable<object> Items { get; set; } = items ?? [];

    /// <summary>
    /// The count of all the entities to be returned by the search (without paging).
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// The arguments to retrieve the next page of items.  The client needs to prepend
    /// the URI of the table to this.
    /// </summary>
    public string? NextLink { get; set; }
}

/// <summary>
/// A paged result with a specific type.
/// </summary>
/// <remarks>
/// This class is used in constructing OpenApi documents.
/// </remarks>
/// <typeparam name="T">The type of the entity.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The list of entities to include in the response.
    /// </summary>
    public required IEnumerable<T> Items { get; set; }

    /// <summary>
    /// The count of all the entities to be returned by the search (without paging).
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// The arguments to retrieve the next page of items.  The client needs to prepend
    /// the URI of the table to this.
    /// </summary>
    public string? NextLink { get; set; }
}
