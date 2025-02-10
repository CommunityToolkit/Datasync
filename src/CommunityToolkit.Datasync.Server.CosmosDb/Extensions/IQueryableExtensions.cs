// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace CommunityToolkit.Datasync.Server.CosmosDb.Extensions;

/// <summary>
/// CosmosDB specific extensions for IQueryable
/// </summary>
public static class IQueryableExtensions
{
    /// <summary>
    /// Converts an IQueryable to a <see cref="List{T}"/> using a <see cref="FeedIterator{T}"/>
    /// </summary>
    /// <param name="queryable">The queryable to convert</param>
    /// <param name="cancellationToken">The cancellation token to use</param>
    /// <typeparam name="TEntity">The type of entity in the queryable</typeparam>
    public static async Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        List<TEntity> list = [];
        using (FeedIterator<TEntity> iterator = queryable.ToFeedIterator())
        {

            while (iterator.HasMoreResults)
            {
                FeedResponse<TEntity> response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

                list.AddRange(response);
            }
        }

        return list;
    }
}
