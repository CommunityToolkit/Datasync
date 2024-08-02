// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Paging;

/// <summary>
/// An implementation of the <see cref="AsyncPageable{T}"/> class that reads
/// each page in a query from a paging function that is supplied on construction.
/// </summary>
internal class FuncAsyncPageable<T>(Func<string?, ValueTask<Page<T>>> pageFunc) : AsyncPageable<T>()
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null)
    {
        do
        {
            Page<T> pageResponse = await pageFunc(continuationToken).ConfigureAwait(false);
            continuationToken = pageResponse.NextLink?.ToString();
            yield return pageResponse;
        } while (continuationToken != null);
    }
}
