// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The default values for various settings within the Datasync client.
/// </summary>
public static class DatasyncClientDefaults
{
    /// <summary>
    /// The default number of parallel operations allowed during a push/pull operation.
    /// </summary>
    public const int ParallelOperations = 1;

    /// <summary>
    /// The maximum number of parallel operations allowed during a push/pull operation.
    /// </summary>
    public const int MaxParallelOperations = 8;

    /// <summary>
    /// The default entity ID generator.
    /// </summary>
    public static string EntityIdGenerator(string _) => Guid.NewGuid().ToString();

    /// <summary>
    /// The function to use for turning a table name into a relative path for accessing 
    /// the table endpoint on the remote service.
    /// </summary>
    public static string TableEndpointResolver(string tableName) => $"/tables/{tableName.ToLowerInvariant()}";
}
