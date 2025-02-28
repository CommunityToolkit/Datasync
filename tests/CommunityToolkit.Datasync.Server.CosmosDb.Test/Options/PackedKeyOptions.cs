// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.CosmosDb.Test.Models;
using Microsoft.Azure.Cosmos;

namespace CommunityToolkit.Datasync.Server.CosmosDb.Test.Options;

public class PackedKeyOptions(string databaseId, string containerId, bool shouldUpdateTimestamp = true)
    : CosmosSingleTableOptions<CosmosDbMovie>(databaseId, containerId, shouldUpdateTimestamp)
{
    public override Func<CosmosDbMovie, string> IdGenerator => (entity) => $"{Guid.NewGuid()}:{entity.Year}";
    public override string GetPartitionKey(CosmosDbMovie entity, out PartitionKey partitionKey)
    {
        partitionKey = new PartitionKey(entity.Year);
        return entity.Id;
    }

    public override string ParsePartitionKey(string id, out PartitionKey partitionKey)
    {
        string[] parts = id.Split(':');

        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid ID format");
        }

        if (!int.TryParse(parts[1], out int year))
        {
            throw new ArgumentException("Invalid ID Part");
        }

        partitionKey = new PartitionKey(year);
        return id;
    }
}
