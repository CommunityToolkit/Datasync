// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A collection of values that may take multiple service requests to iterate over.
/// </summary>
/// <typeparam name="T">The type of the entity being iterated over.</typeparam>
/// <example>
/// Example of enumerating an <see cref="AsyncPageable{T}"/> using the <c>async foreach</c> loop:
/// <code snippet="Snippet:AsyncPageable">
/// IAsyncPageable&lt;Movie&gt; allMovies = dataset.QueryAsync();
/// await foreach (Movie movie in allMovies)
/// {
///   ProcessMovie(movie);
/// }
/// </code>
/// </example>
public interface IAsyncPageable<T> : IAsyncEnumerable<T> where T : notnull
{
    /// <summary>
    /// Enumerate the values a page of items at a time.  This may take multiple service requests.
    /// </summary>
    /// <param name="continuationToken">A continuation tokne indicating where to resume paging, or <c>null</c> to begin paging from the beginning.</param>
    /// <returns>An async sequence of <see cref="Page{T}"/> entities.</returns>
    IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = default);
}
