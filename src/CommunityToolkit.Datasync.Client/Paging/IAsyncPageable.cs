// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A collection of values that may take multiple service requests to iterate over.
/// </summary>
/// <typeparam name="T">The type of the entity being iterated over.</typeparam>
/// <example>
/// Example of enumerating an <see cref="IAsyncPageable{T}"/> using the <c>async foreach</c> loop:
/// <code snippet="Snippet:IAsyncPageable">
/// IAsyncPageable&lt;Movie&gt; allMovies = table.Query();
/// await foreach (Movie movie in allMovies)
/// {
///   ProcessMovie(movie);
/// }
/// </code>
/// </example>
public interface IAsyncPageable<T> : IAsyncEnumerable<T>
{
    /// <summary>
    /// Enumerate the values a <see cref="Page{T}"/> of items at a time.  This may make multiple service requests.
    /// </summary>
    /// <param name="continuationToken">A continuation token indicating where to resume paging, or null to begin paging from the beginning</param>
    /// <returns>An async sequence of <see cref="Page{T}"/>s</returns>
    IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = default);

    /// <summary>
    /// The total number of items that would be returned by the query, if not for paging.
    /// </summary>
    /// <remarks>
    /// This is only populated if the total count is requested on the query.  It is <c>null</c> when this is not the case.
    /// </remarks>
    long? Count { get; }
}
