// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public static class ConnectionStrings
{
    public static readonly string AzureSql = Environment.GetEnvironmentVariable("AZSQL_CONNECTION_STRING");
    public static readonly string CosmosDb = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
    public static readonly string MongoCommunity = Environment.GetEnvironmentVariable("MONGOACI_CONNECTION_STRING");
    public static readonly string CosmosMongo = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
    public static readonly string Service = Environment.GetEnvironmentVariable("SERVICE_ENDPOINT");
    public static readonly bool EnableLogging = (Environment.GetEnvironmentVariable("ENABLE_SQL_LOGGING") ?? "false") == "true";
}