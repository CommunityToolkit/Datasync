// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.CosmosDb;
/// <summary>
/// Base implementation of <see cref="ITableData"/> for entities that are stored in a shared CosmosDB collection.
/// </summary>
/// <typeparam name="T">The type of the Table Data</typeparam>
public abstract class CosmosTableData<T> : CosmosTableData where T : CosmosTableData
{
    /// <summary>
    /// The entity type for the data. Used as the default partition key for shared containers.
    /// </summary>
    public virtual string Entity => typeof(T).Name;
}
