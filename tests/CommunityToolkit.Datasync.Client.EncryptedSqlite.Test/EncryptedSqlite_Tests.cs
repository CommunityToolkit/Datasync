// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.EncryptedSqlite.Test.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite.Test;

[ExcludeFromCodeCoverage]
public class EncryptedSqlite_Tests : EncryptedSqliteTestBase
{
    [Fact]
    public void EncryptedStore_RoundTrips()
    {
        const string password = "R0undTr1p!";

        Seed(password);

        _ = ReadCount(password).Should().Be(1);
    }

    [Fact]
    public void EncryptedStore_FileHasNoPlaintextHeader()
    {
        Seed("Head3rT3st!");

        AssertFileIsEncrypted();
    }

    [Fact]
    public void WrongPassword_Fails()
    {
        Seed("correct-horse-battery-staple");

        Action act = () => ReadCount("the-wrong-password");

        _ = act.Should().Throw<SqliteException>();
    }

    [Fact]
    public void NoPassword_Fails()
    {
        Seed("correct-horse-battery-staple");

        // The provider has been initialized (by Seed), but opening an encrypted database without a key must fail.
        DbContextOptionsBuilder<PlainTodoContext> builder = new();
        _ = builder.UseSqlite(ConnectionString);
        using PlainTodoContext context = new(builder.Options);

        Action act = () => context.TodoItems.Count();

        _ = act.Should().Throw<SqliteException>();
    }

    [Fact]
    public void Rekey_ChangesTheEncryptionKey()
    {
        const string oldPassword = "0ld-K3y!";
        const string newPassword = "N3w-K3y!";
        Seed(oldPassword);

        using (SqliteConnection connection = EncryptedSqliteFactory.CreateConnection(ConnectionString, oldPassword))
        {
            connection.RekeyEncryptedSqlite(newPassword);
        }

        _ = ReadCount(newPassword).Should().Be(1);

        Action readWithOldKey = () => ReadCount(oldPassword);
        _ = readWithOldKey.Should().Throw<SqliteException>();
    }

    [Fact]
    public void SqlCipherCompatibleFormat_RoundTrips()
    {
        const string password = "Sql-C1ph3r!";
        EncryptedSqliteOptions options = new() { Cipher = "sqlcipher", LegacyCompatibility = 4 };

        using (SqliteConnection writeConnection = EncryptedSqliteFactory.CreateConnection(ConnectionString, password, options))
        {
            DbContextOptionsBuilder<PlainTodoContext> builder = new();
            _ = builder.UseEncryptedSqlite(writeConnection);
            using PlainTodoContext context = new(builder.Options);
            _ = context.Database.EnsureCreated();
            _ = context.TodoItems.Add(new TodoItem { Title = "compat" });
            _ = context.SaveChanges();
        }

        AssertFileIsEncrypted();

        using (SqliteConnection readConnection = EncryptedSqliteFactory.CreateConnection(ConnectionString, password, options))
        {
            DbContextOptionsBuilder<PlainTodoContext> builder = new();
            _ = builder.UseEncryptedSqlite(readConnection);
            using PlainTodoContext context = new(builder.Options);
            _ = context.TodoItems.Count().Should().Be(1);
        }
    }
}
