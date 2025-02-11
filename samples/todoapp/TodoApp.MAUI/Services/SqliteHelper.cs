// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;

namespace TodoApp.MAUI.Services;

internal sealed class SqliteHelper
{
    private const string ConnectionString = "Data Source=:memory:";
    private static readonly Lazy<SqliteHelper> singleton = new(() => new SqliteHelper());

    public static SqliteConnection Connection => singleton.Value.SqliteConnection;

    #region SqliteHelper Internals
    private SqliteHelper()
    {
        SqliteConnection = new SqliteConnection(ConnectionString);
        SqliteConnection.Open();
    }

    private SqliteConnection SqliteConnection { get; }
    #endregion
}
