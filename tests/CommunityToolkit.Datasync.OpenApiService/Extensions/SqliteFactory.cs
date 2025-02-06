// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;

namespace CommunityToolkit.Datasync.OpenApiService.Extensions;

public static class SqliteFactory
{
    private static SqliteConnection _connection;
    private static readonly object _lock = new();

    public static SqliteConnection CreateAndOpenConnection(string connectionString = "Data Source=:memory:")
    {
        lock (_lock)
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(connectionString);
                _connection.Open();
            }
        }

        return _connection;
    }
}
