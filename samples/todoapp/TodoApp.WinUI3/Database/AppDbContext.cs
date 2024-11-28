// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define OFFLINESYNC_ENABLED

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.WinUI3.Services;

namespace TodoApp.WinUI3.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
// public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    //protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    //{
    //    HttpClientOptions clientOptions = new()
    //    {
    //        Endpoint = new Uri("https://app-qhvxwauvecrtg.azurewebsites.net/"),
    //        HttpPipeline = [new LoggingHandler()]
    //    };
    //    _ = optionsBuilder.UseHttpClientOptions(clientOptions);
    //}

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        //PushResult pushResult = await this.PushAsync(cancellationToken);
        //if (!pushResult.IsSuccessful)
        //{
        //    throw new ApplicationException($"Push failed: {pushResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        //}

        //PullResult pullResult = await this.PullAsync(cancellationToken);
        //if (!pullResult.IsSuccessful)
        //{
        //    throw new ApplicationException($"Pull failed: {pullResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        //}
    }
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
    {
        _ = context.Database.EnsureCreated();
        // Task.Run(async () => await context.SynchronizeAsync());
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => context.Database.EnsureCreatedAsync(cancellationToken);
}
