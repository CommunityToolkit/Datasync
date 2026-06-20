// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite;

/// <summary>
/// Helpers for building SQLite literals.  PRAGMA statement arguments (such as the encryption key) cannot be supplied
/// as parameters, so the value must be embedded as a properly escaped SQL string literal.  Escaping the value rather
/// than running a query (for example <c>SELECT quote(...)</c>) is important because, when opening an encrypted
/// database, no statement can execute until the key has been set.
/// </summary>
internal static class SqliteLiteral
{
    /// <summary>
    /// Returns <paramref name="value"/> as a single-quoted SQLite string literal, doubling any embedded quotes.
    /// </summary>
    internal static string Quote(string value)
        => $"'{value.Replace("'", "''")}'";
}
