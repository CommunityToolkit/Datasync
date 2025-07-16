// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public static class ConnectionStrings
{
    /// <summary>
    /// The connection string for CosmosDb database with standard NoSQL API.
    /// </summary>
    public static readonly string CosmosDb = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");

    /// <summary>
    /// The connection string for CosmosDb database with MongoDB API.
    /// </summary>
    public static readonly string CosmosMongo = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");

    /// <summary>
    /// If true, enables logging of SQL queries to the console.
    /// </summary>
    public static readonly bool EnableLogging = (Environment.GetEnvironmentVariable("ENABLE_SQL_LOGGING") ?? "false") == "true";
}