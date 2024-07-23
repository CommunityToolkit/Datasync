// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public static class TestDbContextFactory
{
    public static InMemoryOfflineDbContext CreateInMemoryContext()
    {
        DbContextOptionsBuilder<InMemoryOfflineDbContext> optionsBuilder = new();
        optionsBuilder.UseInMemoryDatabase("inmemory");
        return new InMemoryOfflineDbContext(optionsBuilder.Options);
    }
}
