// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.ViewModels;

namespace TodoApp.MAUI.Utils;
internal static class ServicesContainer
{
    private static readonly Lazy<SqliteConnection> dbConnection = new(() => new("Data Source=:memory:"));
    private static readonly Lazy<IServiceProvider> Services = new(() => CreateServiceProvider());

    private static ServiceProvider CreateServiceProvider()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddTransient<MainViewModel>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(dbConnection.Value))
            .BuildServiceProvider();

        using IServiceScope scope = serviceProvider.CreateScope();
        IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        initializer.Initialize();

        return serviceProvider;
    }

    public static TService GetRequiredService<TService>() where TService : notnull
        => Services.Value.GetRequiredService<TService>();
}
