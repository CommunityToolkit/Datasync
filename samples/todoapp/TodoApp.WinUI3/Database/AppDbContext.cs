// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TodoApp.WinUI3.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    //protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    //{
    //    _ = optionsBuilder.UseEndpoint(new Uri("https://myservice.azurewebsites.net"));
    //}
}

/// <summary>
/// Use this class to initialize the database.  In this sample, we just create
/// the database using <see cref="DatabaseFacade.EnsureCreated"/>.  However, you
/// may want to use migrations.
/// </summary>
/// <param name="context">The context for the database.</param>
public class DbContextInitializer(AppDbContext context) : IDbInitializer
{
    /// <inheritdoc />
    public void Initialize()
        => context.Database.EnsureCreated();

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => context.Database.EnsureCreatedAsync(cancellationToken);
}
