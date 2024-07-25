// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Models;

/// <summary>
/// A collection of values that may take multiple service requests to iterate over.
/// </summary>
/// <typeparam name="T">The type of the entity being iterated over.</typeparam>
/// <example>
/// Example of enumerating an <see cref="AsyncPageable{T}"/> using the <c>async foreach</c> loop:
/// <code snippet="Snippet:AsyncPageable">
/// AsyncPageable&lt;Movie&gt; allMovies = dataset.QueryAsync();
/// await foreach (Movie movie in allMovies)
/// {
///   ProcessMovie(movie);
/// }
/// </code>
/// </example>
public abstract class AsyncPageable<T> : IAsyncPageable<T> where T : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncPageable{T}"/> class.
    /// </summary>
    /// <remarks>Used for unit test mocking.</remarks>
    protected AsyncPageable()
    {
    }

    /// <summary>
    /// Enumerate the values a page of items at a time.  This may take multiple service requests.
    /// </summary>
    /// <param name="continuationToken">A continuation token indicating where to resume paging, or <c>null</c> to begin paging from the beginning.</param>
    /// <returns>An async sequence of <see cref="Page{T}"/> entities</returns>
    public abstract IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = default);

    /// <summary>
    /// Enumerate the values in the collection asynchronously.  This may make multiple service requests.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> used for requests made while enumerating asynchronously.</param>
    /// <returns>An async sequence of values.</returns>
    public virtual async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
    {
        await foreach (Page<T> page in AsPages().ConfigureAwait(false).WithCancellation(token))
        {
            Count = page.Count;
            if (page.Items != null)
            {
                foreach (T value in page.Items)
                {
                    yield return value;
                }
            }
        }
    }

    /// <summary>
    /// The total number of items that would be returned by the query, if not for paging.
    /// This is populated only if the total count ($count=true) is requested on the query.
    /// </summary>
    public long? Count { get; private set; }
}

/// <summary>
/// Creates a new <see cref="AsyncPageable{T}"/> with a function iterator.
/// </summary>
/// <param name="pageFunc">The function that gets the next page</param>
internal class FuncAsyncPageable<T>(Func<string?, Task<Page<T>>> pageFunc) : AsyncPageable<T> where T : notnull
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<Page<T>> AsPages(string? requestUri = default)
    {
        do
        {
            Page<T> pageResponse = await pageFunc(requestUri).ConfigureAwait(false);
            requestUri = pageResponse.NextLink?.ToString();
            yield return pageResponse ?? new Page<T>();
        } while (requestUri != null);
    }
}
